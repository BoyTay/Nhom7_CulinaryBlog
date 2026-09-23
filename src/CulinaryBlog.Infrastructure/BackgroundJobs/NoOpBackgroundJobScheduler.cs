using System.Linq.Expressions;
using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class NoOpBackgroundJobScheduler : IBackgroundJobScheduler
{
    public string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall) =>
        string.Empty;

    public void AddOrUpdate<TJob>(
        string recurringJobId,
        Expression<Func<TJob, Task>> methodCall,
        string cronExpression)
    {
    }
}
