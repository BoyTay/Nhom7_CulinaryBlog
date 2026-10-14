namespace CulinaryBlog.Application.Abstractions.Jobs;

/// <summary>Renews the lease owned by the current execution, or throws when ownership is lost.</summary>
public interface IJobLeaseStore : IJobExecutionStore
{
    TimeSpan RenewalInterval { get; }
    Task RenewAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}

public sealed class JobLeaseUnavailableException(string message) : Exception(message);

public sealed class JobExecutionOptions
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(15);
}
