using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CulinaryBlog.Integration.Tests;

public sealed class JobExecutionStoreTests
{
    [Fact]
    public async Task ClaimsAreAtomicFailuresRetryAndCompletedKeysStayDeduplicated()
    {
        await using var database = await TestDatabase.CreateAsync();
        const string key = "integration:job-execution";

        var contexts = Enumerable.Range(0, 12).Select(_ => database.CreateDbContext()).ToArray();
        try
        {
            var stores = contexts.Select(context => new PostgresJobExecutionStore(context)).ToArray();
            var results = await Task.WhenAll(stores.Select(async store =>
            {
                try { return await store.TryClaimAsync(key); }
                catch (CulinaryBlog.Application.Abstractions.Jobs.JobLeaseUnavailableException) { return false; }
            }));
            Assert.Single(results, result => result);
            var owner = stores[Array.IndexOf(results, true)];
            await owner.MarkFailedAsync(key);
            Assert.True(await owner.TryClaimAsync(key));
            await owner.MarkCompletedAsync(key);
            Assert.False(await owner.TryClaimAsync(key));
            Assert.Equal(1, await owner.DeleteExpiredBeforeAsync(DateTimeOffset.UtcNow.AddDays(1)));
            Assert.True(await owner.TryClaimAsync(key));
        }
        finally { foreach (var context in contexts) await context.DisposeAsync(); }
    }

    [Fact]
    public async Task BusyClaimThrowsAndStaleWorkerCannotCompleteOrReleaseSuccessor()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var firstContext = database.CreateDbContext();
        await using var secondContext = database.CreateDbContext();
        var first = new PostgresJobExecutionStore(firstContext);
        var second = new PostgresJobExecutionStore(secondContext);
        const string key = "lease:stale-worker";
        Assert.True(await first.TryClaimAsync(key));
        await Assert.ThrowsAsync<CulinaryBlog.Application.Abstractions.Jobs.JobLeaseUnavailableException>(() => second.TryClaimAsync(key));
        await firstContext.Database.ExecuteSqlRawAsync("UPDATE \"JobExecutions\" SET \"LockedUntil\" = NOW() - interval '1 second'");
        Assert.True(await second.TryClaimAsync(key));
        await Assert.ThrowsAsync<CulinaryBlog.Application.Abstractions.Jobs.JobLeaseUnavailableException>(() => first.MarkCompletedAsync(key));
        await first.MarkFailedAsync(key);
        await second.RenewAsync(key);
        Assert.Equal(0, await second.DeleteExpiredBeforeAsync(DateTimeOffset.UtcNow.AddDays(1)));
        await second.MarkCompletedAsync(key);
        Assert.False(await first.TryClaimAsync(key));
    }

    [Theory]
    [InlineData("20261007190000_AddRecipeFullTextSearch")]
    [InlineData("20261013150052_AddRecipeImageVariants")]
    public async Task MigrationUpgradesPreviousDatabaseAndCanRollBackExecutionStore(string baseline)
    {
        await using var database = await TestDatabase.CreateAsync(baseline);
        await using var context = database.CreateDbContext();
        var migrations = context.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>();
        await context.Database.ExecuteSqlRawAsync("CREATE TABLE \"MigrationSentinel\" (\"Value\" integer PRIMARY KEY); INSERT INTO \"MigrationSentinel\" VALUES (29)");
        await migrations.MigrateAsync();
        Assert.Equal(29, await context.Database.SqlQueryRaw<int>("SELECT \"Value\" FROM \"MigrationSentinel\"").SingleAsync());
        Assert.False(context.Database.HasPendingModelChanges());
        var store = new PostgresJobExecutionStore(context);
        Assert.True(await store.TryClaimAsync("upgrade:test"));
        await store.MarkCompletedAsync("upgrade:test");
        await migrations.MigrateAsync("20261013150052_AddRecipeImageVariants");
        await migrations.MigrateAsync();
        Assert.True(await store.TryClaimAsync("upgrade:test"));
        Assert.Equal(29, await context.Database.SqlQueryRaw<int>("SELECT \"Value\" FROM \"MigrationSentinel\"").SingleAsync());
    }

    private sealed class TestDatabase(
        string connectionString,
        string schema,
        DbContextOptions<ApplicationDbContext> options) : IAsyncDisposable
    {
        public ApplicationDbContext CreateDbContext() => new(options);

        public static async Task<TestDatabase> CreateAsync(string? baseline = null)
        {
            var baseDirectory = AppContext.BaseDirectory;
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(baseDirectory, "appsettings.json"), optional: true)
                .AddJsonFile(Path.Combine(baseDirectory, "appsettings.Development.json"), optional: true)
                .AddEnvironmentVariables()
                .Build();
            var configuredConnectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:Postgres must be configured to run PostgreSQL integration tests.");
            var adminConnectionString = new NpgsqlConnectionStringBuilder(configuredConnectionString)
            {
                Pooling = false,
            }.ConnectionString;
            var schema = $"job_execution_{Guid.NewGuid():N}";

            await using (var connection = new NpgsqlConnection(adminConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText =
                    $"""
                    CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
                    CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;
                    CREATE SCHEMA "{schema}";
                    """;
                await command.ExecuteNonQueryAsync();
            }

            var testConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString)
            {
                SearchPath = schema,
            }.ConnectionString;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(testConnectionString)
                .Options;
            var dbContext = new ApplicationDbContext(options);

            try
            {
                if (baseline is null) await dbContext.Database.MigrateAsync();
                else await dbContext.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>().MigrateAsync(baseline);
            }
            catch
            {
                await dbContext.DisposeAsync();
                await DropSchemaAsync(adminConnectionString, schema);
                throw;
            }

            await dbContext.DisposeAsync();
            return new TestDatabase(adminConnectionString, schema, options);
        }

        public async ValueTask DisposeAsync() =>
            await DropSchemaAsync(connectionString, schema);

        private static async Task DropSchemaAsync(string connectionString, string schema)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE";
            await command.ExecuteNonQueryAsync();
        }
    }
}
