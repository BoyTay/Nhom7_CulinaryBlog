using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CulinaryBlog.Integration.Tests;

public sealed class StartupMigrationTests
{
    [Fact]
    public async Task HostAppliesAllMigrationsToAnEmptyDatabaseWhenTheFlagIsOn()
    {
        await using var scratch = await ScratchSchema.CreateAsync();

        using (var factory = new MigrationFactory(scratch.ConnectionString, applyOnStartup: true))
        {
            using var client = factory.CreateClient();
            var response = await client.GetAsync(new Uri("/api/v1/categories", UriKind.Relative));

            Assert.True(response.IsSuccessStatusCode, $"GET /categories returned {(int)response.StatusCode}");
        }

        Assert.Equal(ScratchSchema.ExpectedMigrationCount(), await scratch.HistoryRowCountAsync());
    }

    [Fact]
    public async Task HostLeavesTheSchemaUntouchedWhenTheFlagIsOff()
    {
        await using var scratch = await ScratchSchema.CreateAsync();

        using (var factory = new MigrationFactory(scratch.ConnectionString, applyOnStartup: false))
        {
            using var client = factory.CreateClient();
            _ = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        }

        Assert.Equal(0, await scratch.HistoryRowCountAsync());
    }

    [Fact]
    public async Task HostKeepsExistingDataWhenAMigratedDatabaseIsStartedAgainWithTheFlagOn()
    {
        await using var scratch = await ScratchSchema.CreateAsync();

        for (var run = 0; run < 2; run++)
        {
            using var factory = new MigrationFactory(scratch.ConnectionString, applyOnStartup: true);
            using var client = factory.CreateClient();
            var response = await client.GetAsync(new Uri("/api/v1/categories", UriKind.Relative));
            Assert.True(response.IsSuccessStatusCode, $"run {run}: {(int)response.StatusCode}");
        }

        Assert.Equal(ScratchSchema.ExpectedMigrationCount(), await scratch.HistoryRowCountAsync());
        Assert.True(await scratch.CategoryCountAsync() > 0, "seeded categories should survive the second start");
    }

    private sealed class MigrationFactory(string connectionString, bool applyOnStartup) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            // UseSetting is visible to Program.cs while it builds the host, unlike ConfigureAppConfiguration.
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Database:ApplyMigrationsOnStartup", applyOnStartup ? "true" : "false");
        }
    }

    private sealed class ScratchSchema : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        private readonly string _schema;

        private ScratchSchema(string adminConnectionString, string schema)
        {
            _adminConnectionString = adminConnectionString;
            _schema = schema;
            ConnectionString = new NpgsqlConnectionStringBuilder(adminConnectionString) { SearchPath = schema }.ConnectionString;
        }

        public string ConnectionString { get; }

        public static int ExpectedMigrationCount()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql("Host=unused").Options;
            using var context = new ApplicationDbContext(options);
            return context.Database.GetMigrations().Count();
        }

        public static async Task<ScratchSchema> CreateAsync()
        {
            using var configuration = new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment("Development"));
            var configured = configuration.Services.GetRequiredService<IConfiguration>().GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
            var admin = new NpgsqlConnectionStringBuilder(configured) { Pooling = false }.ConnectionString;
            var schema = $"startup_migration_{Guid.NewGuid():N}";
            await ExecuteAsync(admin, $"""
                CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
                CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;
                CREATE SCHEMA "{schema}";
                """);
            return new ScratchSchema(admin, schema);
        }

        public async Task<int> HistoryRowCountAsync()
        {
            var tableExists = await ScalarAsync(
                $"SELECT count(*)::int FROM information_schema.tables WHERE table_schema = '{_schema}' AND table_name = '__EFMigrationsHistory'");
            return tableExists == 0
                ? 0
                : await ScalarAsync($"SELECT count(*)::int FROM \"{_schema}\".\"__EFMigrationsHistory\"");
        }

        public async Task<int> CategoryCountAsync() =>
            await ScalarAsync($"SELECT count(*)::int FROM \"{_schema}\".\"Categories\"");

        public async ValueTask DisposeAsync()
        {
            NpgsqlConnection.ClearAllPools();
            await ExecuteAsync(_adminConnectionString, $"DROP SCHEMA IF EXISTS \"{_schema}\" CASCADE");
        }

        private async Task<int> ScalarAsync(string sql)
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            return Convert.ToInt32(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
        }

        private static async Task ExecuteAsync(string connectionString, string sql)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
