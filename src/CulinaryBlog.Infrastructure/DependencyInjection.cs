using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.BackgroundJobs;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Health;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
using CulinaryBlog.Infrastructure.Persistence.Seeders;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CulinaryBlog.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuditableEntityInterceptor>();
        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            options
                .UseNpgsql(connectionString, npgsql =>
                    npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName))
                .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));
        services.AddScoped<IDataSession>(serviceProvider =>
            serviceProvider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IRecipeSearchReader, RecipeSearchReader>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<IJobExecutionStore, PostgresJobExecutionStore>();
        services.AddScoped<WelcomeEmailJob>(serviceProvider =>
            ActivatorUtilities.CreateInstance<WelcomeEmailJob>(serviceProvider));
        services.AddScoped<ThumbnailJob>(serviceProvider =>
            ActivatorUtilities.CreateInstance<ThumbnailJob>(serviceProvider));
        services.AddScoped<SitemapJob>(serviceProvider =>
            ActivatorUtilities.CreateInstance<SitemapJob>(serviceProvider));
        services.AddScoped<JobExecutionCleanupJob>();

        var hangfireEnabled = configuration["Hangfire:Enabled"] is { } hangfireSetting
            ? bool.Parse(hangfireSetting)
            : false;

        if (hangfireEnabled)
        {
            services.AddHangfire(configuration => configuration
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString))
                .UseFilter(new AutomaticRetryAttribute
                {
                    Attempts = 5,
                    DelaysInSeconds = [60, 300, 900, 1800, 3600],
                    OnAttemptsExceeded = AttemptsExceededAction.Fail,
                }));
            services.AddHangfireServer(options =>
            {
                options.Queues = ["critical", "default", "maintenance"];
                options.WorkerCount = Math.Clamp(Environment.ProcessorCount, 1, 20);
            });
            services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
            services.AddHostedService<HangfireRecurringJobRegistrationService>();
            services.AddHealthChecks()
                .AddCheck<HangfireStorageHealthCheck>("hangfire", tags: ["ready"]);
        }
        else
        {
            services.AddSingleton<IBackgroundJobScheduler, NoOpBackgroundJobScheduler>();
        }

        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
            .AddCheck<RedisHealthCheck>("redis", tags: ["ready"])
            .AddCheck<MinioHealthCheck>("minio", tags: ["ready"]);
        services.AddHttpClient("MinioHealth");
        services.AddHttpContextAccessor();

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;

            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ICurrentUser, CurrentUser>();
        services.AddScoped<RoleSeeder>();
        services.AddScoped<CategorySeeder>();
        services.AddHttpClient("GoogleJwks");
        services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        return services;
    }
}
