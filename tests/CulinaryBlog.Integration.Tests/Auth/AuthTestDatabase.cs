using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CulinaryBlog.Integration.Tests.Auth;

internal sealed class AuthTestDatabase(string adminConnection, string schema, string connectionString) : IAsyncDisposable
{
    public string ConnectionString { get; } = connectionString;

    public ApplicationDbContext CreateContext() => new(new DbContextOptionsBuilder<ApplicationDbContext>()
        .UseNpgsql(ConnectionString).Options);

    public static async Task<AuthTestDatabase> CreateAsync(string baseline = "latest")
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables().Build();
        var adminConnection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Postgres"))
        {
            Pooling = false,
        }.ConnectionString;
        var schema = $"auth_security_{Guid.NewGuid():N}";
        await using (var connection = new NpgsqlConnection(adminConnection))
        {
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            // PostgreSQL extensions belong to the database, not the disposable test schema.
            // Serialize initial creation when fixtures start against a completely empty database.
            command.CommandText = $"""
                BEGIN;
                SELECT pg_advisory_xact_lock(3535001);
                CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;
                CREATE EXTENSION IF NOT EXISTS pg_trgm WITH SCHEMA public;
                CREATE SCHEMA "{schema}";
                COMMIT;
                """;
            await command.ExecuteNonQueryAsync();
        }

        var testConnection = new NpgsqlConnectionStringBuilder(adminConnection) { SearchPath = schema }.ConnectionString;
        var database = new AuthTestDatabase(adminConnection, schema, testConnection);
        try
        {
            await using var context = database.CreateContext();
            if (baseline == "latest") await context.Database.MigrateAsync();
            else await context.GetService<IMigrator>().MigrateAsync(baseline);
            return database;
        }
        catch
        {
            await database.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await using var connection = new NpgsqlConnection(adminConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        // schema is generated internally and never comes from configuration or user input.
        command.CommandText = $"DROP SCHEMA IF EXISTS \"{schema}\" CASCADE";
        await command.ExecuteNonQueryAsync();
    }
}
