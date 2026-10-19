using System.Linq.Expressions;
using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.Auth.Events;
using CulinaryBlog.Application.BackgroundJobs;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.Application.Tests;

public sealed class UserRegisteredWelcomeEmailHandlerTests
{
    [Fact]
    public async Task RegistrationEventEnqueuesWelcomeEmailWithStableUserKeyOnCriticalQueue()
    {
        var scheduler = new RecordingJobScheduler();
        var handler = new UserRegisteredWelcomeEmailHandler(
            scheduler,
            NullLogger<UserRegisteredWelcomeEmailHandler>.Instance);
        var notification = new UserRegisteredEvent(
            "user-42",
            "person@example.com",
            "Recipe Fan",
            DateTimeOffset.UtcNow);

        await handler.Handle(notification, CancellationToken.None);

        Assert.Equal(typeof(WelcomeEmailJob), scheduler.JobType);
        Assert.Equal("critical", scheduler.QueueName);
        Assert.NotNull(scheduler.MethodCall);

        var store = new RecordingExecutionStore();
        var sender = new RecordingWelcomeEmailSender();
        var job = new WelcomeEmailJob(sender, store);
        await scheduler.MethodCall.Compile()(job);

        Assert.Equal("welcome-email:user:user-42", store.IdempotencyKey);
        Assert.Equal("person@example.com", sender.Email);
        Assert.Equal("Recipe Fan", sender.DisplayName);
    }

    private sealed class RecordingJobScheduler : IBackgroundJobScheduler
    {
        public Type? JobType { get; private set; }

        public string? QueueName { get; private set; }

        public Expression<Func<WelcomeEmailJob, Task>>? MethodCall { get; private set; }

        public string Enqueue<TJob>(
            Expression<Func<TJob, Task>> methodCall,
            string queueName = "default")
        {
            JobType = typeof(TJob);
            QueueName = queueName;
            MethodCall = (Expression<Func<WelcomeEmailJob, Task>>)(object)methodCall;
            return "test-job-id";
        }

        public void AddOrUpdate<TJob>(
            string recurringJobId,
            Expression<Func<TJob, Task>> methodCall,
            string cronExpression,
            string queueName = "default") =>
            throw new NotSupportedException();
    }

    private sealed class RecordingExecutionStore : IJobExecutionStore
    {
        public string? IdempotencyKey { get; private set; }

        public Task<bool> TryClaimAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            IdempotencyKey = idempotencyKey;
            return Task.FromResult(true);
        }

        public Task MarkCompletedAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task MarkFailedAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<int> DeleteExpiredBeforeAsync(
            DateTimeOffset cutoff,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class RecordingWelcomeEmailSender : IWelcomeEmailSender
    {
        public string? Email { get; private set; }

        public string? DisplayName { get; private set; }

        public Task SendAsync(
            string email,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            Email = email;
            DisplayName = displayName;
            return Task.CompletedTask;
        }
    }
}
