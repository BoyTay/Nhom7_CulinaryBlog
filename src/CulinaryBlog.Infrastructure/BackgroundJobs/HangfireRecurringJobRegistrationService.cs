using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class HangfireRecurringJobRegistrationService(
    IBackgroundJobScheduler scheduler,
    IServiceProviderIsService serviceProviderIsService,
    ILogger<HangfireRecurringJobRegistrationService> logger) : IHostedService
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
            scheduler.AddOrUpdate<SitemapJob>(
                "sitemap-daily",
                job => job.ExecuteRecurringAsync(CancellationToken.None),
                "0 2 * * *",
                "maintenance");
        }
        else
        {
            LogSitemapWriterUnavailable(logger, nameof(ISitemapWriter), null);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
