using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;

namespace CulinaryBlog.Application.Tests;

public sealed class BackgroundJobTests
{
    [Fact]
    public async Task WelcomeEmailJobSkipsDuplicateIdempotencyKey()
    {
        var store = new FakeJobExecutionStore { AlreadyClaimed = true };
        var sender = new FakeWelcomeEmailSender();
        var job = new WelcomeEmailJob(sender, store);

        await job.ExecuteAsync("welcome-email:user:1", "user@example.com", "User");

        Assert.Equal(0, sender.SendCount);
        Assert.Equal(1, store.ClaimCount);
        Assert.Equal(0, store.CompletedCount);
    }

    private sealed class FakeJobExecutionStore : IJobExecutionStore
    {
        public bool AlreadyClaimed { get; init; }

        public int ClaimCount { get; private set; }

        public int CompletedCount { get; private set; }

        public Task<bool> TryClaimAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            ClaimCount++;
            return Task.FromResult(!AlreadyClaimed);
        }

        public Task MarkCompletedAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            CompletedCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeWelcomeEmailSender : IWelcomeEmailSender
    {
        public int SendCount { get; private set; }

        public Task SendAsync(
            string email,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            return Task.CompletedTask;
        }
    }
}
