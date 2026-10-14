using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

public sealed class PostgresJobExecutionStore(ApplicationDbContext dbContext) : IJobExecutionStore
{
    private const int MaximumIdempotencyKeyLength = 256;
    private const int LeaseMinutes = 20;

    public async Task<bool> TryClaimAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyKey(idempotencyKey);

        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "JobExecutions"
                ("IdempotencyKey", "Status", "AttemptCount", "LockedUntil", "LastAttemptAt")
            VALUES
                ({idempotencyKey}, 'Running', 1, NOW() + make_interval(mins => {LeaseMinutes}), NOW())
            ON CONFLICT ("IdempotencyKey") DO UPDATE
            SET "Status" = 'Running',
                "AttemptCount" = "JobExecutions"."AttemptCount" + 1,
                "LockedUntil" = NOW() + make_interval(mins => {LeaseMinutes}),
                "LastAttemptAt" = NOW()
            WHERE "JobExecutions"."Status" <> 'Completed'
              AND (
                    "JobExecutions"."Status" <> 'Running'
                    OR "JobExecutions"."LockedUntil" <= NOW()
              );
            """,
            cancellationToken);

        return affected == 1;
    }

    public async Task MarkCompletedAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyKey(idempotencyKey);

        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "JobExecutions"
            SET "Status" = 'Completed',
                "LockedUntil" = NULL,
                "CompletedAt" = NOW()
            WHERE "IdempotencyKey" = {idempotencyKey}
              AND "Status" = 'Running';
            """,
            cancellationToken);

        if (affected != 1)
        {
            throw new InvalidOperationException(
                $"Job execution '{idempotencyKey}' could not be marked completed because it is not claimed.");
        }
    }

    public async Task MarkFailedAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ValidateIdempotencyKey(idempotencyKey);

        var affected = await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE "JobExecutions"
            SET "Status" = 'Failed',
                "LockedUntil" = NULL
            WHERE "IdempotencyKey" = {idempotencyKey}
              AND "Status" = 'Running';
            """,
            cancellationToken);

        if (affected != 1)
        {
            throw new InvalidOperationException(
                $"Job execution '{idempotencyKey}' could not be marked failed because it is not claimed.");
        }
    }

    public async Task<int> DeleteExpiredBeforeAsync(
        DateTimeOffset cutoff,
        CancellationToken cancellationToken = default) =>
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            DELETE FROM "JobExecutions"
            WHERE ("Status" = 'Completed' AND "CompletedAt" < {cutoff})
               OR ("Status" = 'Failed' AND "LastAttemptAt" < {cutoff})
               OR ("Status" = 'Running' AND "LockedUntil" < {cutoff});
            """,
            cancellationToken);

    private static void ValidateIdempotencyKey(string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (idempotencyKey.Length > MaximumIdempotencyKeyLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(idempotencyKey),
                $"Idempotency keys must not exceed {MaximumIdempotencyKeyLength} characters.");
        }
    }
}
