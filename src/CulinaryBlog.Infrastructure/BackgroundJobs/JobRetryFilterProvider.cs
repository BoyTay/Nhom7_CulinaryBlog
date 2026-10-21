using CulinaryBlog.Application.BackgroundJobs;
using Hangfire;
using Hangfire.Common;
using Hangfire.Server;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class JobRetryFilterProvider(
    HangfireOptions options,
    ILogger<JobRetryFilterProvider> logger) : IJobFilterProvider
{
    public IEnumerable<JobFilter> GetFilters(Job job)
    {
        foreach (var filter in JobFilterProviders.Providers.GetFilters(job))
        {
            if (filter.Instance is not AutomaticRetryAttribute)
            {
                yield return filter;
            }
        }

        var failureLogger = new JobFailureLoggingFilter(logger);
        yield return new JobFilter(failureLogger, JobFilterScope.Global, order: 0);

        var delays = job.Type == typeof(WelcomeEmailJob)
            ? options.WelcomeEmailRetryDelaysSeconds
            : job.Type == typeof(ThumbnailJob)
                ? options.ThumbnailRetryDelaysSeconds
                : job.Type == typeof(SitemapJob) || job.Type == typeof(RecurringSitemapJob)
                    ? options.SitemapRetryDelaysSeconds
                    : options.RetryDelaysSeconds;
        var retry = new AutomaticRetryAttribute
        {
            Attempts = delays.Length,
            DelaysInSeconds = delays,
            OnAttemptsExceeded = AttemptsExceededAction.Fail,
        };
        yield return new JobFilter(retry, JobFilterScope.Global, retry.Order);
    }
}

public sealed class JobFailureLoggingFilter(ILogger logger) : IServerFilter
{
    private static readonly Action<ILogger, string, string, Exception?> LogJobFailure =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(9401, "BackgroundJobFailed"),
            "Background job {JobType} ({JobId}) failed; Hangfire will apply its retry policy.");

    public void OnPerforming(PerformingContext context)
    {
    }

    public void OnPerformed(PerformedContext context)
    {
        if (context.Exception is not null)
        {
            LogJobFailure(
                logger,
                context.BackgroundJob.Job.Type.Name,
                context.BackgroundJob.Id,
                context.Exception);
        }
    }
}
