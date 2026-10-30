using System.Runtime.CompilerServices;
using Npgsql;

namespace CulinaryBlog.Integration.Tests;

/// <summary>
/// Several integration tests write real rows (see <see cref="RecipeIntegrationTestData"/>) into the shared development
/// database. This removes exactly the rows those helpers create when the test process ends, so repeated test runs no
/// longer fill the site with "Recipe test category" entries. Only runs when ConnectionStrings__Postgres is set.
/// </summary>
internal static class TestDataCleanup
{
    private const string Sql = """
        begin;
        delete from "Recipes" where "AuthorId" like 'recipe-test-author-%';
        delete from "AspNetUsers" where "Id" like 'recipe-test-author-%';
        delete from "Categories" where "Name" like 'Recipe test category %' and not exists (select 1 from "Recipes" r where r."CategoryId" = "Categories"."Id");
        commit;
        """;

    [ModuleInitializer]
    internal static void Register() => AppDomain.CurrentDomain.ProcessExit += (_, _) => Run();

    internal static void Run()
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Postgres");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return;
        }

        try
        {
            using var connection = new NpgsqlConnection(connectionString);
            connection.Open();
            using var command = new NpgsqlCommand(Sql, connection);
            command.ExecuteNonQuery();
        }
        catch (Exception exception) when (exception is NpgsqlException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Test data cleanup skipped: {exception.Message}");
        }
    }
}
