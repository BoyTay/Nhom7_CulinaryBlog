using CulinaryBlog.Application.BackgroundJobs;
using Hangfire;
using Hangfire.Common;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class JobRetryFilterProvider(HangfireOptions options) : IJobFilterProvider
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
