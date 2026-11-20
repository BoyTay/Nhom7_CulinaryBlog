using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;
using Hangfire;
using Hangfire.Server;
using Hangfire.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class HangfireRecurringJobRegistrationService(
    IBackgroundJobScheduler scheduler,
    IServiceProviderIsService serviceProviderIsService,
    ILogger<HangfireRecurringJobRegistrationService> logger,
    IRecurringJobManager? recurringJobManager = null) : IHostedService
{
    private static readonly Action<ILogger, string, Exception?> LogSitemapWriterUnavailable =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(9001, "SitemapWriterUnavailable"),
            "Daily sitemap job was not registered because no {SitemapWriter} implementation is available.");

    private static readonly Action<ILogger, string, Exception?> LogRecurringJobLocked =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(9002, "RecurringJobRegistrationLocked"),
            "Recurring job {RecurringJobId} was not re-registered because its Hangfire lock is held; "
            + "the existing registration is kept and the next start will retry.");

    public Task StartAsync(CancellationToken cancellationToken)
    {
        TryRegister("job-execution-cleanup", () =>
            scheduler.AddOrUpdate<JobExecutionCleanupJob>(
                "job-execution-cleanup",
                job => job.ExecuteAsync(CancellationToken.None),
                "0 3 * * *",
                "maintenance"));

        if (serviceProviderIsService.IsService(typeof(ISitemapWriter)))
        {
            TryRegister("sitemap-daily", () =>
                scheduler.AddOrUpdate<RecurringSitemapJob>(
                    "sitemap-daily",
                    job => job.ExecuteAsync(null, CancellationToken.None),
                    "0 2 * * *",
                    "maintenance"));
        }
        else
        {
            recurringJobManager?.RemoveIfExists("sitemap-daily");
            LogSitemapWriterUnavailable(logger, nameof(ISitemapWriter), null);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    // A stale row in hangfire.lock (left by an instance stopped mid-registration) must not
    // crash the host: Hangfire.PostgreSql only expires it after its lock timeout.
    private void TryRegister(string recurringJobId, Action register)
    {
        try
        {
            register();
        }
        catch (DistributedLockTimeoutException exception)
        {
            LogRecurringJobLocked(logger, recurringJobId, exception);
        }
    }
}

public sealed class RecurringSitemapJob(SitemapJob job)
{
    public Task ExecuteAsync(PerformContext? context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        // CreatedAt and job ID survive retries across UTC midnight and manual retries.
        return job.ExecuteAsync($"sitemap:{context.BackgroundJob.CreatedAt:yyyy-MM-dd}:{context.BackgroundJob.Id}", cancellationToken);
    }
}
