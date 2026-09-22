using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Health;
using CulinaryBlog.Infrastructure.Persistence;
using CulinaryBlog.Infrastructure.Persistence.Interceptors;
using Hangfire;
using Hangfire.PostgreSql;
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
        if (bool.TryParse(configuration["Hangfire:Enabled"], out var hangfireEnabled) && hangfireEnabled)
        {
            services.AddHangfire(configuration => configuration
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options => options.UseNpgsqlConnection(connectionString)));
            services.AddHangfireServer();
            services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        }
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}
