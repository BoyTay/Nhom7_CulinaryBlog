namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class HangfireOptions
{
    private static readonly string[] RequiredQueues = ["critical", "default", "maintenance"];
    public bool Enabled { get; set; }
    public int WorkerCount { get; set; } = 2;
    public string[] Queues { get; set; } = ["critical", "default", "maintenance"];
    public int[] RetryDelaysSeconds { get; set; } = [60, 300, 900, 1800, 3600];
    public int[] WelcomeEmailRetryDelaysSeconds { get; set; } = [60, 300, 1800];
    public int[] ThumbnailRetryDelaysSeconds { get; set; } = [60, 300, 1800];
    public int[] SitemapRetryDelaysSeconds { get; set; } = [60, 300];
    public int TimeoutSeconds { get; set; } = 900;
    public int LeaseSeconds { get; set; } = 1200;
    public int RenewalSeconds { get; set; } = 30;
    public int RetentionDays { get; set; } = 90;
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    public void Validate()
    {
        if (WorkerCount < 1 || TimeoutSeconds < 1 || LeaseSeconds < 3 || RenewalSeconds < 1
            || (long)RenewalSeconds * 2 >= LeaseSeconds || RetentionDays < 1 || ShutdownTimeoutSeconds < 1
            || !AreRetryDelaysValid(RetryDelaysSeconds)
            || !AreRetryDelaysValid(WelcomeEmailRetryDelaysSeconds)
            || !AreRetryDelaysValid(ThumbnailRetryDelaysSeconds)
            || !AreRetryDelaysValid(SitemapRetryDelaysSeconds)
            || Queues.Length == 0 || Queues.Any(queue => string.IsNullOrEmpty(queue)
                || queue.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '_')))
            || RequiredQueues.Except(Queues, StringComparer.Ordinal).Any())
            throw new InvalidOperationException("Invalid Hangfire worker, queue, retry, timeout, lease or retention settings.");
    }

    private static bool AreRetryDelaysValid(int[] delays) =>
        delays.Length > 0 && delays.All(delay => delay > 0);
}
