using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;

namespace CulinaryBlog.Application.Tests;

public sealed class BackgroundJobTests
{
    private static readonly string[] ClaimOnlyEvents = ["claim"];
    private static readonly string[] WelcomeCompletedEvents = ["claim", "welcome", "complete"];
    private static readonly string[] WelcomeRetriedEvents = ["claim", "welcome", "failed", "claim", "welcome", "complete"];
    private static readonly string[] ThumbnailCompletedEvents = ["claim", "thumbnail", "complete"];
    private static readonly string[] ThumbnailRetriedEvents = ["claim", "thumbnail", "failed", "claim", "thumbnail", "complete"];
    private static readonly string[] SitemapCompletedEvents = ["claim", "sitemap", "complete"];
    private static readonly string[] SitemapRetriedEvents = ["claim", "sitemap", "failed", "claim", "sitemap", "complete"];

    [Fact]
    public async Task IdempotentJobExecutionMarksTimedOutOperationRetryable()
    {
        var store = new FakeJobExecutionStore(true);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            IdempotentJobExecution.RunAsync(
                store,
                "test:timeout",
                token => Task.Delay(Timeout.InfiniteTimeSpan, token),
                timeout: TimeSpan.FromMilliseconds(10)));

        Assert.Equal(["claim", "failed"], store.Events);
    }

    [Fact]
    public async Task WelcomeEmailJobCompletesAfterSending()
    {
        var store = new FakeJobExecutionStore(true);
        var sender = new FakeWelcomeEmailSender(store);
        var job = new WelcomeEmailJob(sender, store);

        await job.ExecuteAsync("welcome-email:user:1", "user@example.com", "User");

        Assert.Equal(1, sender.SendCount);
        Assert.Equal(WelcomeCompletedEvents, store.Events);
    }

    [Fact]
    public async Task WelcomeEmailJobSkipsDuplicateIdempotencyKey()
    {
        var store = new FakeJobExecutionStore(false);
        var sender = new FakeWelcomeEmailSender(store);
        var job = new WelcomeEmailJob(sender, store);

        await job.ExecuteAsync("welcome-email:user:1", "user@example.com", "User");

        Assert.Equal(0, sender.SendCount);
        Assert.Equal(ClaimOnlyEvents, store.Events);
    }

    [Fact]
    public async Task WelcomeEmailJobLeavesFailedDeliveryRetryable()
    {
        var store = new FakeJobExecutionStore(true, true);
        var sender = new FakeWelcomeEmailSender(store) { ShouldFail = true };
        var job = new WelcomeEmailJob(sender, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteAsync("welcome-email:user:1", "user@example.com", "User"));

        sender.ShouldFail = false;
        await job.ExecuteAsync("welcome-email:user:1", "user@example.com", "User");

        Assert.Equal(2, sender.SendCount);
        Assert.Equal(WelcomeRetriedEvents, store.Events);
    }

    [Fact]
    public async Task ThumbnailJobCompletesAfterProcessing()
    {
        var store = new FakeJobExecutionStore(true);
        var processor = new FakeRecipeImageProcessor(store);
        var job = new ThumbnailJob(processor, store);

        await job.ExecuteAsync("thumbnail:image:1", Guid.NewGuid(), "https://example.com/image.jpg");

        Assert.Equal(1, processor.ProcessCount);
        Assert.Equal(ThumbnailCompletedEvents, store.Events);
    }

    [Fact]
    public async Task ThumbnailJobSkipsDuplicateIdempotencyKey()
    {
        var store = new FakeJobExecutionStore(false);
        var processor = new FakeRecipeImageProcessor(store);
        var job = new ThumbnailJob(processor, store);

        await job.ExecuteAsync("thumbnail:image:1", Guid.NewGuid(), "https://example.com/image.jpg");

        Assert.Equal(0, processor.ProcessCount);
        Assert.Equal(ClaimOnlyEvents, store.Events);
    }

    [Fact]
    public async Task ThumbnailJobLeavesFailedProcessingRetryable()
    {
        var store = new FakeJobExecutionStore(true, true);
        var processor = new FakeRecipeImageProcessor(store) { ShouldFail = true };
        var job = new ThumbnailJob(processor, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteAsync("thumbnail:image:1", Guid.NewGuid(), "https://example.com/image.jpg"));

        processor.ShouldFail = false;
        await job.ExecuteAsync("thumbnail:image:1", Guid.NewGuid(), "https://example.com/image.jpg");

        Assert.Equal(2, processor.ProcessCount);
        Assert.Equal(ThumbnailRetriedEvents, store.Events);
    }

    [Fact]
    public async Task SitemapJobCompletesAfterWriting()
    {
        var store = new FakeJobExecutionStore(true);
        var writer = new FakeSitemapWriter(store);
        var job = new SitemapJob(writer, store, TimeProvider.System);

        await job.ExecuteAsync("sitemap:2026-09-23");

        Assert.Equal(1, writer.GenerateCount);
        Assert.Equal(SitemapCompletedEvents, store.Events);
    }

    [Fact]
    public async Task SitemapJobSkipsDuplicateIdempotencyKey()
    {
        var store = new FakeJobExecutionStore(false);
        var writer = new FakeSitemapWriter(store);
        var job = new SitemapJob(writer, store, TimeProvider.System);

        await job.ExecuteAsync("sitemap:2026-09-23");

        Assert.Equal(0, writer.GenerateCount);
        Assert.Equal(ClaimOnlyEvents, store.Events);
    }

    [Fact]
    public async Task SitemapJobLeavesFailedWritingRetryable()
    {
        var store = new FakeJobExecutionStore(true, true);
        var writer = new FakeSitemapWriter(store) { ShouldFail = true };
        var job = new SitemapJob(writer, store, TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteAsync("sitemap:2026-09-23"));

        writer.ShouldFail = false;
        await job.ExecuteAsync("sitemap:2026-09-23");

        Assert.Equal(2, writer.GenerateCount);
        Assert.Equal(SitemapRetriedEvents, store.Events);
    }

    [Fact]
    public async Task RenewalFailureCancelsOperationAndNeverCompletes()
    {
        var store = new RenewingStore { LoseLease = true };
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => IdempotentJobExecution.RunAsync(
            store, "lease:lost", token => Task.Delay(Timeout.InfiniteTimeSpan, token)));
        Assert.True(store.RenewCount > 0);
        Assert.False(store.Completed);
        Assert.True(store.Failed);
    }

    [Fact]
    public async Task TimeoutKeepsLeaseUntilNonCooperativeOperationActuallyStops()
    {
        var store = new RenewingStore();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => IdempotentJobExecution.RunAsync(
            store, "lease:slow", async _ => await Task.Delay(80, CancellationToken.None), TimeSpan.FromMilliseconds(10)));
        Assert.True(store.RenewCount > 0);
        Assert.False(store.Completed);
        Assert.True(store.Failed);
    }

    private sealed class RenewingStore : IJobLeaseStore
    {
        public TimeSpan RenewalInterval => TimeSpan.FromMilliseconds(5);
        public bool LoseLease { get; set; }
        public int RenewCount { get; private set; }
        public bool Completed { get; private set; }
        public bool Failed { get; private set; }
        public Task<bool> TryClaimAsync(string idempotencyKey, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task RenewAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        {
            RenewCount++;
            if (LoseLease) throw new JobLeaseUnavailableException("Test lease lost.");
            return Task.CompletedTask;
        }
        public Task MarkCompletedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        { Completed = true; return Task.CompletedTask; }
        public Task MarkFailedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
        { Failed = true; return Task.CompletedTask; }
        public Task<int> DeleteExpiredBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class FakeJobExecutionStore : IJobExecutionStore
    {
        private readonly Queue<bool> claimResults;

        public FakeJobExecutionStore(params bool[] claimResults)
        {
            this.claimResults = new Queue<bool>(claimResults);
        }

        public List<string> Events { get; } = [];

        public void Record(string eventName) => Events.Add(eventName);

        public Task<bool> TryClaimAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            Events.Add("claim");
            return Task.FromResult(claimResults.Dequeue());
        }

        public Task MarkCompletedAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            Events.Add("complete");
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(
            string idempotencyKey,
            CancellationToken cancellationToken = default)
        {
            Events.Add("failed");
            return Task.CompletedTask;
        }

        public Task<int> DeleteExpiredBeforeAsync(
            DateTimeOffset cutoff,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }

    private sealed class FakeWelcomeEmailSender : IWelcomeEmailSender
    {
        private readonly FakeJobExecutionStore store;

        public FakeWelcomeEmailSender(FakeJobExecutionStore store)
        {
            this.store = store;
        }

        public int SendCount { get; private set; }

        public bool ShouldFail { get; set; }

        public Task SendAsync(
            string email,
            string displayName,
            CancellationToken cancellationToken = default)
        {
            SendCount++;
            store.Record("welcome");
            if (ShouldFail)
            {
                throw new InvalidOperationException("Delivery failed.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeRecipeImageProcessor : IRecipeImageProcessor
    {
        private readonly FakeJobExecutionStore store;

        public FakeRecipeImageProcessor(FakeJobExecutionStore store)
        {
            this.store = store;
        }

        public int ProcessCount { get; private set; }

        public bool ShouldFail { get; set; }

        public Task ProcessAsync(
            Guid recipeImageId,
            string originalUrl,
            CancellationToken cancellationToken = default)
        {
            ProcessCount++;
            store.Record("thumbnail");
            if (ShouldFail)
            {
                throw new InvalidOperationException("Processing failed.");
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeSitemapWriter : ISitemapWriter
    {
        private readonly FakeJobExecutionStore store;

        public FakeSitemapWriter(FakeJobExecutionStore store)
        {
            this.store = store;
        }

        public int GenerateCount { get; private set; }

        public bool ShouldFail { get; set; }

        public Task GenerateAsync(CancellationToken cancellationToken = default)
        {
            GenerateCount++;
            store.Record("sitemap");
            if (ShouldFail)
            {
                throw new InvalidOperationException("Sitemap generation failed.");
            }

            return Task.CompletedTask;
        }
    }
}
