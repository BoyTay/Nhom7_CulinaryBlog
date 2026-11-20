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

    [Fact]
    public async Task ContentFileOnAnEmptyDatabaseImportsOnlyItsCategoriesWithResolvedImages()
    {
        await using var scratch = await ScratchSchema.CreateAsync();
        var contentFile = Path.Combine(Path.GetTempPath(), $"content-{Guid.NewGuid():N}.json");
        await File.WriteAllTextAsync(contentFile, """
            {
              "categories": [
                { "name": "Món canh", "slug": "mon-canh-test", "description": "Canh", "orderIndex": 0, "imageUrl": "canh-chua.jpg" }
              ],
              "recipes": [
                {
                  "title": "Canh chua", "slug": "canh-chua-test", "description": "Canh chua cá.", "categorySlug": "mon-canh-test",
                  "prepTimeMinutes": 10, "cookTimeMinutes": 20, "servings": 4, "difficulty": "Easy", "imageUrl": "canh-chua.jpg",
                  "ingredients": [ { "name": "Cá", "quantity": 400, "unit": "gr", "notes": null } ],
                  "steps": [
                    { "description": "Sơ chế cá.", "timerMinutes": null, "imageUrl": "steps/canh-chua-1.jpg" },
                    { "description": "Nấu canh.", "timerMinutes": null }
                  ]
                }
              ]
            }
            """);
        try
        {
            var settings = new Dictionary<string, string>
            {
                ["Seed:ContentFile"] = contentFile,
                ["PublicSite:BaseUrl"] = "https://blog.example.test",
                ["Admin:Email"] = "content-admin@example.test",
                ["Admin:Password"] = "Content#Admin2026",
            };
            using (var factory = new MigrationFactory(scratch.ConnectionString, applyOnStartup: true, settings))
            {
                using var client = factory.CreateClient();
                var response = await client.GetAsync(new Uri("/api/v1/categories", UriKind.Relative));
                Assert.True(response.IsSuccessStatusCode, $"GET /categories returned {(int)response.StatusCode}");
            }

            // The development placeholder categories must not be mixed into imported real content.
            var categories = await scratch.CategoriesAsync();
            var category = Assert.Single(categories);
            Assert.Equal("mon-canh-test", category.Slug);
            Assert.Equal("https://blog.example.test/images/recipes/canh-chua.jpg", category.ImageUrl);

            // Step images use the same relative-name rule as recipe images; steps without one stay empty.
            Assert.Equal(
                ["https://blog.example.test/images/recipes/steps/canh-chua-1.jpg", null],
                await scratch.StepImageUrlsAsync());
        }
        finally
        {
            File.Delete(contentFile);
        }
    }

    private sealed class MigrationFactory(
        string connectionString,
        bool applyOnStartup,
        IReadOnlyDictionary<string, string>? settings = null) : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            // UseSetting is visible to Program.cs while it builds the host, unlike ConfigureAppConfiguration.
            builder.UseSetting("ConnectionStrings:Postgres", connectionString);
            builder.UseSetting("Database:ApplyMigrationsOnStartup", applyOnStartup ? "true" : "false");
            foreach (var (key, value) in settings ?? new Dictionary<string, string>())
            {
                builder.UseSetting(key, value);
            }
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

        public async Task<List<(string Slug, string? ImageUrl)>> CategoriesAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"SELECT \"Slug\", \"ImageUrl\" FROM \"{_schema}\".\"Categories\" ORDER BY \"Slug\"", connection);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<(string Slug, string? ImageUrl)>();
            while (await reader.ReadAsync())
            {
                rows.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
            }

            return rows;
        }

        public async Task<List<string?>> StepImageUrlsAsync()
        {
            await using var connection = new NpgsqlConnection(_adminConnectionString);
            await connection.OpenAsync();
            await using var command = new NpgsqlCommand(
                $"SELECT \"ImageUrl\" FROM \"{_schema}\".\"RecipeSteps\" ORDER BY \"StepNumber\"", connection);
            await using var reader = await command.ExecuteReaderAsync();
            var rows = new List<string?>();
            while (await reader.ReadAsync())
            {
                rows.Add(reader.IsDBNull(0) ? null : reader.GetString(0));
            }

            return rows;
        }

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
