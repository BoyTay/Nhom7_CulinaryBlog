using CulinaryBlog.Infrastructure.BackgroundJobs;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
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

        var concurrentClaims = await Task.WhenAll(
            Enumerable.Range(0, 12).Select(async _ =>
            {
                await using var dbContext = database.CreateDbContext();
                return await new PostgresJobExecutionStore(dbContext).TryClaimAsync(key);
            }));

        Assert.Single(concurrentClaims, claim => claim);

        await using (var dbContext = database.CreateDbContext())
        {
            var store = new PostgresJobExecutionStore(dbContext);
            await store.MarkFailedAsync(key);
            Assert.True(await store.TryClaimAsync(key));
            await store.MarkCompletedAsync(key);
            Assert.False(await store.TryClaimAsync(key));

            var removed = await store.DeleteExpiredBeforeAsync(DateTimeOffset.UtcNow.AddDays(1));
            Assert.Equal(1, removed);
            Assert.True(await store.TryClaimAsync(key));
        }
    }

    private sealed class TestDatabase(
        string connectionString,
        string schema,
        DbContextOptions<ApplicationDbContext> options) : IAsyncDisposable
    {
        public ApplicationDbContext CreateDbContext() => new(options);

        public static async Task<TestDatabase> CreateAsync()
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
                await dbContext.Database.MigrateAsync();
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
