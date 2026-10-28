using System.Globalization;
using System.Text;
using CulinaryBlog.API.Authorization;
using CulinaryBlog.API.Endpoints;
using CulinaryBlog.API.ErrorHandling;
using CulinaryBlog.API.Middleware;
using CulinaryBlog.API.Observability;
using CulinaryBlog.API.Security;
using CulinaryBlog.Application;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Seeders;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Formatting.Compact;

var builder = WebApplication.CreateBuilder(args);
var hangfireEnabled = builder.Configuration["Hangfire:Enabled"] is { } hangfireSetting
    ? bool.Parse(hangfireSetting)
    : false;

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "CulinaryBlog.API")
        .WriteTo.Console(new RenderedCompactJsonFormatter());

    var seqUrl = context.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrWhiteSpace(seqUrl))
    {
        loggerConfiguration.WriteTo.Seq(seqUrl, formatProvider: CultureInfo.InvariantCulture);
    }
}, preserveStaticLogger: true);

builder.Services
    .AddApplication()
    .AddInfrastructure(builder.Configuration);

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = !string.IsNullOrWhiteSpace(jwtSecretKey)
            ? new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
            : null,
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "CulinaryBlog.API",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "CulinaryBlog.Web",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1),
    };
});

builder.Services.AddAuthorization(Policies.ConfigureAuthorization);
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, RecipeAuthorizationHandler>();

// Behind a reverse proxy every request arrives from the proxy address. The default trusts loopback only, so
// Compose opts in with ForwardedHeaders__TrustAllProxies=true; the API port is not published there, which is
// what makes trusting X-Forwarded-For safe. Never enable it when clients can reach the API directly.
if (builder.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

builder.Services.AddRateLimiter(options => AuthRateLimit.Configure(
    options,
    AuthRateLimit.ResolvePermitLimit(builder.Configuration["RateLimiting:AuthPermitLimit"])));

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://127.0.0.1:3000"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .WithExposedHeaders("ETag", "Location", "Set-Cookie");
    });
});

builder.Services.AddProblemDetails();
// RecipeStatus and RecipeDifficulty travel as names ("Published", "Easy"), matching the SRS and the web client.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks().AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
builder.Services.AddObservability(builder.Configuration);

var app = builder.Build();

if (app.Configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies"))
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    // Use this host's logger rather than the process-wide static Log.Logger, which another host can replace.
    options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();
    options.EnrichDiagnosticContext = RequestLogEnricher.Enrich;
});
app.UseExceptionHandler();
app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

if (hangfireEnabled)
{
    app.UseHangfireDashboard(
        "/hangfire",
        new DashboardOptions
        {
            Authorization =
            [
                new HangfireAdminDashboardAuthorizationFilter(
                    app.Services.GetRequiredService<IAuthorizationService>()),
            ],
        });
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/scalar");
}

app.MapApiEndpoints();
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = HealthResponseWriter.WriteAsync,
});
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("live"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => !registration.Tags.Contains("live"),
    ResponseWriter = HealthResponseWriter.WriteAsync,
});

// Opt-in (compose sets it): apply pending EF migrations before seeding so a fresh volume comes up usable.
// Failures are not swallowed; an API on a half-migrated schema should not report healthy.
if (app.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();
}

try
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<RoleSeeder>();
    await seeder.SeedAsync();
    if (app.Environment.IsDevelopment())
    {
        await scope.ServiceProvider.GetRequiredService<CategorySeeder>().SeedAsync();
    }
}
catch (Exception ex)
{
    Program.LogSeederWarning(app.Logger, ex);
}

app.Run();

public partial class Program
{
    private static readonly Action<Microsoft.Extensions.Logging.ILogger, Exception?> LogSeederFailed =
        LoggerMessage.Define(LogLevel.Warning, new EventId(8001, "SeederFailed"), "Could not run role seeder at startup.");

    internal static void LogSeederWarning(Microsoft.Extensions.Logging.ILogger logger, Exception ex) =>
        LogSeederFailed(logger, ex);
}
