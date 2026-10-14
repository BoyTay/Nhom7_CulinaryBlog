using System.Runtime.ExceptionServices;
using CulinaryBlog.Application.Abstractions.Jobs;

namespace CulinaryBlog.Application.BackgroundJobs;

public static class IdempotentJobExecution
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(15);

    public static async Task RunAsync(
        IJobExecutionStore executionStore,
        string idempotencyKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(executionStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentNullException.ThrowIfNull(operation);

        var executionTimeout = timeout ?? DefaultTimeout;
        if (executionTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Job timeout must be positive.");
        }

        if (!await executionStore.TryClaimAsync(idempotencyKey, cancellationToken))
        {
            return;
        }

        using var timeoutSource = new CancellationTokenSource(executionTimeout);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        try
        {
            await operation(linkedSource.Token);
            linkedSource.Token.ThrowIfCancellationRequested();
            await executionStore.MarkCompletedAsync(idempotencyKey, CancellationToken.None);
        }
        catch (Exception operationException)
        {
            try
            {
                await executionStore.MarkFailedAsync(idempotencyKey, CancellationToken.None);
            }
            catch (Exception stateException)
            {
                throw new AggregateException(
                    "The job failed and its retry state could not be persisted.",
                    operationException,
                    stateException);
            }

            ExceptionDispatchInfo.Capture(operationException).Throw();
            throw;
        }
    }
}
