namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class JobExecutionRecord
{
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string Status { get; private set; } = string.Empty;
    public int AttemptCount { get; private set; }
    public Guid ClaimToken { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }
    public DateTimeOffset LastAttemptAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
}
