using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Health;
using CulinaryBlog.Infrastructure.Persistence;
using Hangfire;
using Hangfire.PostgreSql;
using Hangfire.Storage;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Npgsql;

namespace CulinaryBlog.Integration.Tests;

public sealed class HangfireRuntimeTests : IAsyncLifetime, IAsyncDisposable
{
    private const string TestJwtKey = "w05-tests-only-key-at-least-sixty-four-characters-never-for-production";
    private string adminConnection = string.Empty;
    private string databaseName = string.Empty;
    private RuntimeFactory factory = null!;

    public async Task InitializeAsync()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.Development.json"), optional: true)
            .AddEnvironmentVariables().Build();
        adminConnection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Configure ConnectionStrings__Postgres for Hangfire runtime tests."))
        { Pooling = false }.ConnectionString;
        databaseName = $"hangfire_w05_{Guid.NewGuid():N}";
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var create = connection.CreateCommand();
        create.CommandText = $"CREATE DATABASE \"{databaseName}\"";
        await create.ExecuteNonQueryAsync();
        try
        {
            var testConnection = new NpgsqlConnectionStringBuilder(adminConnection) { Database = databaseName }.ConnectionString;
            await using var context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(testConnection).Options);
            await context.Database.MigrateAsync();
            factory = new RuntimeFactory(testConnection);
        }
        catch { await DropDatabaseAsync(); throw; }
    }

    ValueTask IAsyncDisposable.DisposeAsync() => new(DisposeAsync());

    public async Task DisposeAsync()
    {
        if (factory is not null) await factory.DisposeAsync();
        await DropDatabaseAsync();
    }

    private async Task DropDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var drop = connection.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\"";
        await drop.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task DashboardUsesRealJwtAdminPolicyAndProtectsMutations()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        using var anonymous = await client.GetAsync("/hangfire");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Author"));
        using var forbidden = await client.GetAsync("/hangfire");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
        using var post = await client.PostAsync("/hangfire/jobs/enqueued", new StringContent("jobs[]=1"));
        Assert.Equal(HttpStatusCode.Forbidden, post.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token("Admin"));
        using var allowed = await client.GetAsync("/hangfire");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
        Assert.Contains("Hangfire", await allowed.Content.ReadAsStringAsync(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task WorkerExecutesThreeJobsRetriesFailureAndPersistsUtcRecurrence()
    {
        using var client = factory.CreateClient();
        using var scope = factory.Services.CreateScope();
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<WelcomeEmailJob>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<ThumbnailJob>());
        Assert.NotNull(scope.ServiceProvider.GetRequiredService<SitemapJob>());
        var scheduler = factory.Services.GetRequiredService<IBackgroundJobScheduler>();
        var storage = factory.Services.GetRequiredService<JobStorage>();
        var effects = factory.Services.GetRequiredService<TestJobServices>();
        effects.FailNextEmail = true;
        var key = $"welcome:runtime:{Guid.NewGuid():N}";
        var email = scheduler.Enqueue<WelcomeEmailJob>(job => job.ExecuteAsync(key, "test@example.com", "Test", CancellationToken.None), "critical");
        var thumbnail = scheduler.Enqueue<ThumbnailJob>(job => job.ExecuteAsync("thumbnail:runtime", Guid.Empty, Guid.Empty, "https://example.com/image.jpg", CancellationToken.None));
        var sitemap = scheduler.Enqueue<SitemapJob>(job => job.ExecuteAsync("sitemap:runtime", CancellationToken.None), "maintenance");
        await WaitAsync(() => Succeeded(storage, email) && Succeeded(storage, thumbnail) && Succeeded(storage, sitemap));
        Assert.Equal(2, effects.EmailAttempts);
        Assert.Equal(1, effects.Thumbnails);
        Assert.Equal(1, effects.Sitemaps);
        var duplicate = scheduler.Enqueue<WelcomeEmailJob>(job => job.ExecuteAsync(key, "test@example.com", "Test", CancellationToken.None));
        await WaitAsync(() => Succeeded(storage, duplicate));
        Assert.Equal(2, effects.EmailAttempts);
        using var connection = storage.GetConnection();
        var recurring = connection.GetRecurringJobs();
        var daily = Assert.Single(recurring, job => job.Id == "sitemap-daily");
        Assert.Equal("0 2 * * *", daily.Cron);
        Assert.Equal(TimeZoneInfo.Utc.Id, daily.TimeZoneId);
        var health = await new HangfireStorageHealthCheck(storage).CheckHealthAsync(new HealthCheckContext());
        Assert.Equal(HealthStatus.Healthy, health.Status);
    }

    [Fact]
    public async Task PersistedQueueSurvivesHostRestart()
    {
        using (var client = factory.CreateClient())
        {
            Assert.NotNull(factory.Services.GetRequiredService<JobStorage>());
        }
        var connectionString = factory.ConnectionString;
        await factory.DisposeAsync();
        // The scheduler remains usable while no worker is running.
        using (var provider = CreateSchedulerProvider(connectionString))
        {
            var scheduler = provider.GetRequiredService<IBackgroundJobScheduler>();
            var jobId = scheduler.Enqueue<SitemapJob>(job => job.ExecuteAsync("sitemap:restart", CancellationToken.None), "maintenance");
            var storage = provider.GetRequiredService<JobStorage>();
            Assert.Equal("Enqueued", storage.GetMonitoringApi().JobDetails(jobId).History[0].StateName);
            Assert.Equal(HealthStatus.Unhealthy,
                (await new HangfireStorageHealthCheck(storage).CheckHealthAsync(new HealthCheckContext())).Status);
            factory = new RuntimeFactory(connectionString);
            using var restarted = factory.CreateClient();
            await WaitAsync(() => Succeeded(storage, jobId));
            Assert.Equal(1, factory.Services.GetRequiredService<TestJobServices>().Sitemaps);
        }
    }

    private static ServiceProvider CreateSchedulerProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHangfire(config => config.UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
        services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        return services.BuildServiceProvider();
    }

    private static bool Succeeded(JobStorage storage, string jobId)
    {
        using var connection = storage.GetConnection();
        return connection.GetStateData(jobId)?.Name == "Succeeded";
    }

    private static async Task WaitAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        while (!condition()) await Task.Delay(200, timeout.Token);
    }

    private static string Token(string role) => new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
        issuer: "CulinaryBlog.API", audience: "CulinaryBlog.Web",
        claims: [new Claim(ClaimTypes.NameIdentifier, "runtime-test"), new Claim(ClaimTypes.Role, role)],
        expires: DateTime.UtcNow.AddMinutes(5),
        signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey)), SecurityAlgorithms.HmacSha256)));

    private sealed class RuntimeFactory(string connectionString) : WebApplicationFactory<Program>
    {
        public string ConnectionString => connectionString;
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Hangfire:Enabled", "true");
            builder.UseSetting("Hangfire:RetryDelaysSeconds:0", "1");
            builder.UseSetting("Jwt:SecretKey", TestJwtKey);
            builder.UseSetting("Seq:ServerUrl", "");
            builder.ConfigureServices(services =>
            {
                services.AddDataProtection().UseEphemeralDataProtectionProvider();
                services.AddSingleton<TestJobServices>();
                services.AddSingleton<IWelcomeEmailSender>(provider => provider.GetRequiredService<TestJobServices>());
                services.AddSingleton<IRecipeImageProcessor>(provider => provider.GetRequiredService<TestJobServices>());
                services.AddSingleton<ISitemapWriter>(provider => provider.GetRequiredService<TestJobServices>());
            });
        }
    }

    private sealed class TestJobServices : IWelcomeEmailSender, IRecipeImageProcessor, ISitemapWriter
    {
        private int emailAttempts;
        private int thumbnails;
        private int sitemaps;
        public bool FailNextEmail { get; set; }
        public int EmailAttempts => Volatile.Read(ref emailAttempts);
        public int Thumbnails => Volatile.Read(ref thumbnails);
        public int Sitemaps => Volatile.Read(ref sitemaps);
        public Task SendAsync(string email, string displayName, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref emailAttempts);
            if (FailNextEmail) { FailNextEmail = false; throw new InvalidOperationException("Injected transient email failure."); }
            return Task.CompletedTask;
        }
        public Task ProcessAsync(
            Guid recipeId,
            Guid recipeImageId,
            string originalUrl,
            CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref thumbnails); return Task.CompletedTask; }
        public Task GenerateAsync(CancellationToken cancellationToken = default)
        { Interlocked.Increment(ref sitemaps); return Task.CompletedTask; }
    }
}
