using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;
using Hangfire;
using Hangfire.Server;
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

    public Task StartAsync(CancellationToken cancellationToken)
    {
        scheduler.AddOrUpdate<JobExecutionCleanupJob>(
            "job-execution-cleanup",
            job => job.ExecuteAsync(CancellationToken.None),
            "0 3 * * *",
            "maintenance");

        if (serviceProviderIsService.IsService(typeof(ISitemapWriter)))
        {
            scheduler.AddOrUpdate<RecurringSitemapJob>(
                "sitemap-daily",
                job => job.ExecuteAsync(null, CancellationToken.None),
                "0 2 * * *",
                "maintenance");
        }
        else
        {
            recurringJobManager?.RemoveIfExists("sitemap-daily");
            LogSitemapWriterUnavailable(logger, nameof(ISitemapWriter), null);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
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
