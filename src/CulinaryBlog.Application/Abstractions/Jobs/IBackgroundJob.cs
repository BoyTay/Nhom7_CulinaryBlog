namespace CulinaryBlog.Application.Abstractions.Jobs;

using System.Linq.Expressions;

public interface IBackgroundJobScheduler
{
    string Enqueue<TJob>(Expression<Func<TJob, Task>> methodCall);

    void AddOrUpdate<TJob>(
        string recurringJobId,
        Expression<Func<TJob, Task>> methodCall,
        string cronExpression);
}
