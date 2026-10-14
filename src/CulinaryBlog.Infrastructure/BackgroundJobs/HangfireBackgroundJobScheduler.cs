using System.Linq.Expressions;
using CulinaryBlog.Application.Abstractions.Jobs;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class HangfireBackgroundJobScheduler(IBackgroundJobClient backgroundJobClient)
    : IBackgroundJobScheduler
{
    public string Enqueue<TJob>(
        Expression<Func<TJob, Task>> methodCall,
        string queueName = "default")
    {
        ArgumentNullException.ThrowIfNull(methodCall);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        return backgroundJobClient.Create(
            Job.FromExpression(methodCall),
            new EnqueuedState(queueName));
    }

    public void AddOrUpdate<TJob>(
        string recurringJobId,
        Expression<Func<TJob, Task>> methodCall,
        string cronExpression,
        string queueName = "default")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recurringJobId);
        ArgumentNullException.ThrowIfNull(methodCall);
        ArgumentException.ThrowIfNullOrWhiteSpace(cronExpression);
        ArgumentException.ThrowIfNullOrWhiteSpace(queueName);

        RecurringJob.AddOrUpdate(
            recurringJobId,
            methodCall,
            cronExpression,
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.Utc,
                QueueName = queueName,
            });
    }
}
