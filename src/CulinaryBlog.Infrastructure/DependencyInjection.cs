using Amazon.Runtime;
using Amazon.S3;
using CulinaryBlog.Application.Abstractions.FileStorage;
using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.BackgroundJobs;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.FileStorage;
using CulinaryBlog.Infrastructure.Health;
using CulinaryBlog.Infrastructure.Identity;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
using CulinaryBlog.Infrastructure.Persistence.Seeders;
using Hangfire;
using Hangfire.Common;
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
        services.AddScoped<IWelcomeEmailSender, SmtpWelcomeEmailSender>();
        services.AddScoped<IRecipeImageProcessor, RecipeImageProcessor>();
        services.AddScoped<ISitemapWriter, SitemapWriter>();
        services.AddSingleton<IAmazonS3>(serviceProvider =>
        {
            var minioConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var endpoint = minioConfiguration["Minio:Endpoint"]
                ?? throw new InvalidOperationException("MinIO endpoint is not configured.");
            var endpointWithScheme = endpoint.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                ? endpoint
                : $"http://{endpoint}";

            return new AmazonS3Client(
                new BasicAWSCredentials(
                    minioConfiguration["Minio:AccessKey"]
                        ?? throw new InvalidOperationException("MinIO access key is not configured."),
                    minioConfiguration["Minio:SecretKey"]
                        ?? throw new InvalidOperationException("MinIO secret key is not configured.")),
                new AmazonS3Config
                {
                    ServiceURL = endpointWithScheme,
                    ForcePathStyle = true,
                    AuthenticationRegion = minioConfiguration["Minio:Region"] ?? "us-east-1",
                });
        });
        services.AddSingleton<IFileStorageService, MinioFileStorageService>();

        var jobOptions = configuration.GetSection("Hangfire").Get<HangfireOptions>() ?? new HangfireOptions();
        jobOptions.Queues = configuration.GetSection("Hangfire:Queues").Get<string[]>() ?? jobOptions.Queues;
        jobOptions.RetryDelaysSeconds = configuration.GetSection("Hangfire:RetryDelaysSeconds").Get<int[]>() ?? jobOptions.RetryDelaysSeconds;
        jobOptions.Validate();
        services.AddSingleton(jobOptions);
        services.AddSingleton(new JobExecutionOptions { Timeout = TimeSpan.FromSeconds(jobOptions.TimeoutSeconds) });
        services.AddScoped<IJobExecutionStore, PostgresJobExecutionStore>();
        services.AddScoped<WelcomeEmailJob>(serviceProvider =>
            ActivatorUtilities.CreateInstance<WelcomeEmailJob>(serviceProvider));
        services.AddScoped<ThumbnailJob>(serviceProvider =>
            ActivatorUtilities.CreateInstance<ThumbnailJob>(serviceProvider));
        services.AddScoped<SitemapJob>(serviceProvider =>
            ActivatorUtilities.CreateInstance<SitemapJob>(serviceProvider));
        services.AddScoped<JobExecutionCleanupJob>();
        services.AddScoped<RecurringSitemapJob>();

        var hangfireEnabled = configuration["Hangfire:Enabled"] is { } hangfireSetting
            ? bool.Parse(hangfireSetting)
            : false;

        if (hangfireEnabled)
        {
            services.AddHangfire(configuration => configuration
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
            services.AddSingleton<IJobFilterProvider, JobRetryFilterProvider>();
            services.AddHangfireServer(options =>
            {
                options.Queues = jobOptions.Queues;
                options.WorkerCount = jobOptions.WorkerCount;
                options.ShutdownTimeout = TimeSpan.FromSeconds(jobOptions.ShutdownTimeoutSeconds);
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
