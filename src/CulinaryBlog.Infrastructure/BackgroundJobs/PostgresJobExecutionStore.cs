using CulinaryBlog.Application.Abstractions.Jobs;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CulinaryBlog.Infrastructure.BackgroundJobs;

// Each DI scope represents one job execution and keeps its own fencing tokens.
public sealed class PostgresJobExecutionStore(ApplicationDbContext dbContext, HangfireOptions? options = null) : IJobLeaseStore
{
    private readonly HangfireOptions settings = options ?? new HangfireOptions();
    private readonly Dictionary<string, Guid> claims = new(StringComparer.Ordinal);
    public TimeSpan RenewalInterval => TimeSpan.FromSeconds(settings.RenewalSeconds);

    public async Task<bool> TryClaimAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        ValidateKey(idempotencyKey);
        var token = Guid.NewGuid();
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO "JobExecutions"
                ("IdempotencyKey", "Status", "AttemptCount", "LockedUntil", "LastAttemptAt", "ClaimToken")
            VALUES (@key, 'Running', 1, clock_timestamp() + @lease, clock_timestamp(), @token)
            ON CONFLICT ("IdempotencyKey") DO UPDATE
            SET "Status" = 'Running', "AttemptCount" = "JobExecutions"."AttemptCount" + 1,
                "LockedUntil" = clock_timestamp() + @lease, "LastAttemptAt" = clock_timestamp(), "ClaimToken" = @token
            WHERE "JobExecutions"."Status" <> 'Completed'
                AND ("JobExecutions"."Status" = 'Failed' OR "JobExecutions"."LockedUntil" <= clock_timestamp())
            RETURNING "ClaimToken";
            """;
        command.Parameters.AddWithValue("key", idempotencyKey);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("lease", TimeSpan.FromSeconds(settings.LeaseSeconds));
        if (await command.ExecuteScalarAsync(cancellationToken) is Guid)
        {
            claims[idempotencyKey] = token;
            return true;
        }
        command.CommandText = """SELECT "Status" FROM "JobExecutions" WHERE "IdempotencyKey" = @key""";
        if (await command.ExecuteScalarAsync(cancellationToken) is string status && status == "Completed") return false;
        // Busy is not success: Hangfire must retry rather than acknowledge lost work.
        throw new JobLeaseUnavailableException("Another execution owns the job lease; retry later.");
    }

    public Task RenewAsync(string idempotencyKey, CancellationToken cancellationToken = default) =>
        UpdateAsync(idempotencyKey, "\"LockedUntil\" = clock_timestamp() + @lease", true, cancellationToken);

    public async Task MarkCompletedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(idempotencyKey, "\"Status\" = 'Completed', \"LockedUntil\" = NULL, \"CompletedAt\" = clock_timestamp()", true, cancellationToken);
        claims.Remove(idempotencyKey);
    }

    public async Task MarkFailedAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        await UpdateAsync(idempotencyKey, "\"Status\" = 'Failed', \"LockedUntil\" = NULL", false, cancellationToken);
        claims.Remove(idempotencyKey);
    }

    public async Task<int> DeleteExpiredBeforeAsync(DateTimeOffset cutoff, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            DELETE FROM "JobExecutions"
            WHERE ("Status" = 'Completed' AND "CompletedAt" < @cutoff)
               OR ("Status" = 'Failed' AND "LastAttemptAt" < @cutoff)
               OR ("Status" = 'Running' AND "LockedUntil" < @cutoff AND "LockedUntil" < clock_timestamp());
            """;
        command.Parameters.AddWithValue("cutoff", cutoff);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task UpdateAsync(string key, string assignment, bool requireLease, CancellationToken cancellationToken)
    {
        ValidateKey(key);
        if (!claims.TryGetValue(key, out var token)) throw new JobLeaseUnavailableException("Execution does not own the job lease.");
        await using var connection = await OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        // assignment is one of the fixed SQL fragments above, never user input.
        command.CommandText = $"UPDATE \"JobExecutions\" SET {assignment} WHERE \"IdempotencyKey\" = @key AND \"ClaimToken\" = @token AND \"Status\" = 'Running'"
            + (requireLease ? " AND \"LockedUntil\" > clock_timestamp()" : string.Empty);
        command.Parameters.AddWithValue("key", key);
        command.Parameters.AddWithValue("token", token);
        command.Parameters.AddWithValue("lease", TimeSpan.FromSeconds(settings.LeaseSeconds));
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1 && requireLease)
            throw new JobLeaseUnavailableException("Job lease expired or was taken by another execution.");
    }

    private async Task<NpgsqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        // Separate connections let the renewal heartbeat run while the job uses its EF context.
        var connection = new NpgsqlConnection(dbContext.Database.GetConnectionString());
        try { await connection.OpenAsync(cancellationToken); return connection; }
        catch { await connection.DisposeAsync(); throw; }
    }

    private static void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        if (key.Length > 256) throw new ArgumentOutOfRangeException(nameof(key), "Idempotency keys must not exceed 256 characters.");
    }
}
