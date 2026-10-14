using Hangfire;
using Hangfire.Common;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

// Per-host settings must not accumulate in Hangfire's process-wide global filters.
public sealed class JobRetryFilterProvider(HangfireOptions options) : IJobFilterProvider
{
    private readonly AutomaticRetryAttribute retry = new()
    {
        Attempts = options.RetryDelaysSeconds.Length,
        DelaysInSeconds = options.RetryDelaysSeconds,
        OnAttemptsExceeded = AttemptsExceededAction.Fail,
    };

    public IEnumerable<JobFilter> GetFilters(Job job)
    {
        foreach (var filter in JobFilterProviders.Providers.GetFilters(job))
            if (filter.Instance is not AutomaticRetryAttribute) yield return filter;
        yield return new JobFilter(retry, JobFilterScope.Global, retry.Order);
    }
}
