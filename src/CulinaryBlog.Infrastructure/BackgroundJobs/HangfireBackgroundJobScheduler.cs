using System.Linq.Expressions;
using CulinaryBlog.Application.Abstractions.Jobs;
using Hangfire;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class HangfireBackgroundJobScheduler : IBackgroundJobScheduler
{
    public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall) =>
        BackgroundJob.Enqueue(methodCall);

    public void AddOrUpdate<TJob>(
        string recurringJobId,
        Expression<Func<TJob, Task>> methodCall,
        string cronExpression)
    {
        RecurringJob.AddOrUpdate(
            recurringJobId,
            methodCall,
            cronExpression,
            new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });
    }
}
