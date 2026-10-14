using System.Linq.Expressions;
using System.Security.Claims;
using CulinaryBlog.API.Authorization;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.BackgroundJobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CulinaryBlog.Integration.Tests;

public sealed class BackgroundJobInfrastructureTests
{
    [Fact]
    public void DisabledHangfireRegistersNoOpSchedulerWithoutStartingAWorker()
    {
        var services = CreateInfrastructureServices(hangfireEnabled: false);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IBackgroundJobScheduler)
                && descriptor.ImplementationType == typeof(NoOpBackgroundJobScheduler));
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(HangfireRecurringJobRegistrationService));
    }

    [Fact]
    public void EnabledHangfireRegistersPersistentSchedulerAndRecurringRegistration()
    {
        var services = CreateInfrastructureServices(hangfireEnabled: true);

        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IBackgroundJobScheduler)
                && descriptor.ImplementationType == typeof(HangfireBackgroundJobScheduler));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService)
                && descriptor.ImplementationType == typeof(HangfireRecurringJobRegistrationService));
        Assert.Contains(
            services,
            descriptor => descriptor.ServiceType == typeof(IHostedService));
    }

    [Fact]
    public async Task RecurringRegistrationUsesUtcSitemapAndMaintenanceQueueWhenWriterExists()
    {
        var scheduler = new RecordingJobScheduler();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISitemapWriter, TestSitemapWriter>();
        using var provider = services.BuildServiceProvider();
        var registration = new HangfireRecurringJobRegistrationService(
            scheduler,
            provider.GetRequiredService<IServiceProviderIsService>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HangfireRecurringJobRegistrationService>>());

        await registration.StartAsync(CancellationToken.None);

        Assert.Collection(
            scheduler.RecurringJobs,
            cleanup =>
            {
                Assert.Equal("job-execution-cleanup", cleanup.Id);
                Assert.Equal("0 3 * * *", cleanup.Cron);
                Assert.Equal("maintenance", cleanup.Queue);
            },
            sitemap =>
            {
                Assert.Equal("sitemap-daily", sitemap.Id);
                Assert.Equal("0 2 * * *", sitemap.Cron);
                Assert.Equal("maintenance", sitemap.Queue);
            });
    }

    [Fact]
    public async Task RecurringRegistrationDoesNotScheduleUnavailableSitemapWriter()
    {
        var scheduler = new RecordingJobScheduler();
        var services = new ServiceCollection();
        services.AddLogging();
        using var provider = services.BuildServiceProvider();
        var registration = new HangfireRecurringJobRegistrationService(
            scheduler,
            provider.GetRequiredService<IServiceProviderIsService>(),
            provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<HangfireRecurringJobRegistrationService>>());

        await registration.StartAsync(CancellationToken.None);

        var cleanup = Assert.Single(scheduler.RecurringJobs);
        Assert.Equal("job-execution-cleanup", cleanup.Id);
    }

    [Fact]
    public void DashboardAccessRequiresAuthenticatedAdminRole()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(Policies.ConfigureAuthorization);
        using var provider = services.BuildServiceProvider();
        var authorizationService = provider.GetRequiredService<IAuthorizationService>();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
        var author = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "author"), new Claim(ClaimTypes.Role, "Author")],
            "test"));
        var unauthenticatedAdmin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Admin")]));
        var admin = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, "Admin")],
            "test"));

        Assert.False(HangfireDashboardAccess.IsAdmin(anonymous, authorizationService));
        Assert.False(HangfireDashboardAccess.IsAdmin(unauthenticatedAdmin, authorizationService));
        Assert.False(HangfireDashboardAccess.IsAdmin(author, authorizationService));
        Assert.True(HangfireDashboardAccess.IsAdmin(admin, authorizationService));
    }

    [Theory]
    [InlineData(0, 30, 1200)]
    [InlineData(1, 600, 1200)]
    [InlineData(1, 30, 30)]
    public void RejectsInvalidWorkerOrLeaseSettings(int workers, int renewal, int lease)
    {
        var options = new HangfireOptions { WorkerCount = workers, RenewalSeconds = renewal, LeaseSeconds = lease };
        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ExplicitRetryArrayReplacesDefaultsInsteadOfAppending()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=culinary_blog;Username=culinary",
            ["Hangfire:RetryDelaysSeconds:0"] = "1",
            ["Hangfire:RetryDelaysSeconds:1"] = "2",
            ["Hangfire:TimeoutSeconds"] = "45",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        var settings = Assert.IsType<HangfireOptions>(Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(HangfireOptions)).ImplementationInstance);
        Assert.Equal([1, 2], settings.RetryDelaysSeconds);
        var execution = Assert.IsType<CulinaryBlog.Application.Abstractions.Jobs.JobExecutionOptions>(
            Assert.Single(services, descriptor => descriptor.ServiceType == typeof(CulinaryBlog.Application.Abstractions.Jobs.JobExecutionOptions)).ImplementationInstance);
        Assert.Equal(TimeSpan.FromSeconds(45), execution.Timeout);
    }

    private sealed class RecordingJobScheduler : IBackgroundJobScheduler
    {
        public List<RecurringRegistration> RecurringJobs { get; } = [];

        public string Enqueue<TJob>(
            Expression<Func<TJob, Task>> methodCall,
            string queueName = "default") =>
            throw new NotSupportedException();

        public void AddOrUpdate<TJob>(
            string recurringJobId,
            Expression<Func<TJob, Task>> methodCall,
            string cronExpression,
            string queueName = "default") =>
            RecurringJobs.Add(new RecurringRegistration(recurringJobId, cronExpression, queueName));
    }

    private sealed record RecurringRegistration(string Id, string Cron, string Queue);

    private sealed class TestSitemapWriter : ISitemapWriter
    {
        public Task GenerateAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private static ServiceCollection CreateInfrastructureServices(bool hangfireEnabled)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=localhost;Port=5432;Database=culinary_blog;Username=culinary",
                ["Hangfire:Enabled"] = hangfireEnabled.ToString(),
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        return services;
    }
}
