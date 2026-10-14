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
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
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
        using var renewalStop = new CancellationTokenSource();
        var renewal = executionStore is IJobLeaseStore leaseStore
            ? RenewAsync(leaseStore, idempotencyKey, linkedSource, renewalStop.Token)
            : Task.CompletedTask;

        try
        {
            await operation(linkedSource.Token);
            renewalStop.Cancel();
            await renewal;
            linkedSource.Token.ThrowIfCancellationRequested();
            using var completionTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await executionStore.MarkCompletedAsync(idempotencyKey, completionTimeout.Token);
        }
        catch (Exception operationException)
        {
            renewalStop.Cancel();
            try { await renewal; }
            catch (JobLeaseUnavailableException) { /* The operation has already failed. */ }
            try
            {
                using var stateTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await executionStore.MarkFailedAsync(idempotencyKey, stateTimeout.Token);
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

    private static async Task RenewAsync(IJobLeaseStore store, string key,
        CancellationTokenSource execution, CancellationToken stop)
    {
        try
        {
            while (true)
            {
                await Task.Delay(store.RenewalInterval, stop);
                await store.RenewAsync(key, stop);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        catch (Exception exception)
        {
            execution.Cancel();
            throw new JobLeaseUnavailableException($"Job lease renewal failed: {exception.GetType().Name}.");
        }
    }
}
