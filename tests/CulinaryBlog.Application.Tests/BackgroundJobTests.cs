using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Application.BackgroundJobs;

namespace CulinaryBlog.Application.Tests;

public sealed class BackgroundJobTests
{
    [Fact]
    public async Task WelcomeEmailJobCompletesAfterSending()
    {
        var store = new FakeJobExecutionStore(true);
        var sender = new FakeWelcomeEmailSender(store);
        var job = new WelcomeEmailJob(sender, store);

        await job.ExecuteAsync("welcome-email:user:1", "user@example.com", "User");

        Assert.Equal(1, sender.SendCount);
        Assert.Equal(new[] { "claim", "welcome", "complete" }, store.Events);
    }

    [Fact]
    public async Task WelcomeEmailJobSkipsDuplicateIdempotencyKey()
    {
        var store = new FakeJobExecutionStore(false);
        var sender = new FakeWelcomeEmailSender(store);
        var job = new WelcomeEmailJob(sender, store);

        await job.ExecuteAsync("welcome-email:user:1", "user@example.com", "User");

        Assert.Equal(0, sender.SendCount);
        Assert.Equal(new[] { "claim" }, store.Events);
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
        Assert.Equal(new[] { "claim", "welcome", "claim", "welcome", "complete" }, store.Events);
    }

    [Fact]
    public async Task ThumbnailJobCompletesAfterProcessing()
    {
        var store = new FakeJobExecutionStore(true);
        var processor = new FakeRecipeImageProcessor(store);
        var job = new ThumbnailJob(processor, store);

        await job.ExecuteAsync("thumbnail:image:1", Guid.NewGuid(), "https://example.com/image.jpg");

        Assert.Equal(1, processor.ProcessCount);
        Assert.Equal(new[] { "claim", "thumbnail", "complete" }, store.Events);
    }

    [Fact]
    public async Task ThumbnailJobSkipsDuplicateIdempotencyKey()
    {
        var store = new FakeJobExecutionStore(false);
        var processor = new FakeRecipeImageProcessor(store);
        var job = new ThumbnailJob(processor, store);

        await job.ExecuteAsync("thumbnail:image:1", Guid.NewGuid(), "https://example.com/image.jpg");

        Assert.Equal(0, processor.ProcessCount);
        Assert.Equal(new[] { "claim" }, store.Events);
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
        Assert.Equal(new[] { "claim", "thumbnail", "claim", "thumbnail", "complete" }, store.Events);
    }

    [Fact]
    public async Task SitemapJobCompletesAfterWriting()
    {
        var store = new FakeJobExecutionStore(true);
        var writer = new FakeSitemapWriter(store);
        var job = new SitemapJob(writer, store);

        await job.ExecuteAsync("sitemap:2026-09-23");

        Assert.Equal(1, writer.GenerateCount);
        Assert.Equal(new[] { "claim", "sitemap", "complete" }, store.Events);
    }

    [Fact]
    public async Task SitemapJobSkipsDuplicateIdempotencyKey()
    {
        var store = new FakeJobExecutionStore(false);
        var writer = new FakeSitemapWriter(store);
        var job = new SitemapJob(writer, store);

        await job.ExecuteAsync("sitemap:2026-09-23");

        Assert.Equal(0, writer.GenerateCount);
        Assert.Equal(new[] { "claim" }, store.Events);
    }

    [Fact]
    public async Task SitemapJobLeavesFailedWritingRetryable()
    {
        var store = new FakeJobExecutionStore(true, true);
        var writer = new FakeSitemapWriter(store) { ShouldFail = true };
        var job = new SitemapJob(writer, store);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            job.ExecuteAsync("sitemap:2026-09-23"));

        writer.ShouldFail = false;
        await job.ExecuteAsync("sitemap:2026-09-23");

        Assert.Equal(2, writer.GenerateCount);
        Assert.Equal(new[] { "claim", "sitemap", "claim", "sitemap", "complete" }, store.Events);
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
