using System.Net;
using System.Text.Json;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeSearchPostgresTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public RecipeSearchPostgresTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SearchEndpointUsesVietnameseFtsAndTrigramFallbackAndKeepsVisibilityRules()
    {
        var categoryId = Guid.NewGuid();
        var token = Guid.NewGuid().ToString("N");
        var published = CreatePublishedRecipe(
            $"Bún bò Huế {token}",
            "A Vietnamese beef noodle soup.",
            categoryId);
        var draft = Recipe.Create(
            $"Bún bò Huế draft {token}",
            $"bun-bo-hue-draft-{token}",
            "A draft recipe.",
            categoryId,
            "search-test",
            10,
            30,
            2,
            RecipeDifficulty.Easy);
        var deleted = CreatePublishedRecipe(
            $"Bún bò Huế deleted {token}",
            "A deleted Vietnamese recipe.",
            categoryId);
        deleted.Delete(DateTimeOffset.UtcNow);
        var fuzzyMatch = CreatePublishedRecipe("mango", "A ripe mango.", categoryId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Recipes.AddRange(published, draft, deleted, fuzzyMatch);
            await dbContext.SaveChangesAsync();
        }

        var unaccentedResults = await SearchAsync("bun bo hue", categoryId);
        Assert.Contains(published.Id, unaccentedResults);
        Assert.DoesNotContain(draft.Id, unaccentedResults);
        Assert.DoesNotContain(deleted.Id, unaccentedResults);

        var trigramResults = await SearchAsync("mangoo", categoryId);
        Assert.Contains(fuzzyMatch.Id, trigramResults);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedTitle = $"Phở gà {token}";
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Recipes\" SET \"Title\" = {updatedTitle} WHERE \"Id\" = {published.Id}");
        }

        var oldTitleResults = await SearchAsync("bun bo hue", categoryId);
        Assert.DoesNotContain(published.Id, oldTitleResults);
        var updatedTitleResults = await SearchAsync("pho ga", categoryId);
        Assert.Contains(published.Id, updatedTitleResults);
    }

    [Fact]
    public async Task SearchGinIndexesAreInstalledAndPostgresProducesIndexedExplainPlans()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        await dbContext.Database.ExecuteSqlRawAsync("SET LOCAL enable_seqscan = off");
        var ginIndexes = await GetGinIndexDefinitionsAsync(dbContext);

        Assert.Equal("gin", ginIndexes["IX_Recipes_SearchVector"].AccessMethod);
        Assert.Contains(
            "\"SearchVector\"",
            ginIndexes["IX_Recipes_SearchVector"].Definition,
            StringComparison.Ordinal);
        Assert.Equal("gin", ginIndexes["IX_Recipes_Title_Trgm"].AccessMethod);
        Assert.Contains(
            "\"Title\" gin_trgm_ops",
            ginIndexes["IX_Recipes_Title_Trgm"].Definition,
            StringComparison.Ordinal);

        var fullTextPlan = await ExplainAsync(
            dbContext,
            """
            EXPLAIN (COSTS OFF)
            SELECT r."Id"
            FROM "Recipes" AS r
            WHERE r."Status" = 'Published'
              AND r."IsDeleted" = FALSE
              AND r."SearchVector" @@ websearch_to_tsquery('culinary_vietnamese', @searchTerm)
            """,
            "bun bo hue");
        var trigramPlan = await ExplainAsync(
            dbContext,
            """
            EXPLAIN (COSTS OFF)
            SELECT r."Id"
            FROM "Recipes" AS r
            WHERE r."Status" = 'Published'
              AND r."IsDeleted" = FALSE
              AND r."Title" % @searchTerm
            """,
            "mangoo");

        Assert.Contains("Index", fullTextPlan, StringComparison.Ordinal);
        Assert.Contains("Index", trigramPlan, StringComparison.Ordinal);
    }

    private static async Task<Dictionary<string, SearchIndexDefinition>> GetGinIndexDefinitionsAsync(
        ApplicationDbContext dbContext)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """
            SELECT index_class.relname,
                   access_method.amname,
                   pg_get_indexdef(index_class.oid)
            FROM pg_class AS table_class
            JOIN pg_index AS index_metadata ON index_metadata.indrelid = table_class.oid
            JOIN pg_class AS index_class ON index_class.oid = index_metadata.indexrelid
            JOIN pg_am AS access_method ON access_method.oid = index_class.relam
            WHERE table_class.relname = 'Recipes'
              AND index_class.relname = ANY(@indexNames)
            """;
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.Parameters.Add(new NpgsqlParameter("indexNames", NpgsqlDbType.Array | NpgsqlDbType.Text)
        {
            Value = new[] { "IX_Recipes_SearchVector", "IX_Recipes_Title_Trgm" },
        });

        var indexes = new Dictionary<string, SearchIndexDefinition>(StringComparer.Ordinal);
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            indexes.Add(
                reader.GetString(0),
                new SearchIndexDefinition(reader.GetString(1), reader.GetString(2)));
        }

        return indexes;
    }

    private async Task<IReadOnlyList<Guid>> SearchAsync(string searchTerm, Guid categoryId)
    {
        using var response = await _client.GetAsync(
            $"/api/v1/recipes/search?q={Uri.EscapeDataString(searchTerm)}&categoryId={categoryId}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return payload.RootElement
            .GetProperty("items")
            .EnumerateArray()
            .Select(item => item.GetProperty("id").GetGuid())
            .ToArray();
    }

    private static Recipe CreatePublishedRecipe(string title, string description, Guid categoryId)
    {
        var recipe = Recipe.Create(
            title,
            $"recipe-{Guid.NewGuid():N}",
            description,
            categoryId,
            "search-test",
            10,
            30,
            2,
            RecipeDifficulty.Easy);
        recipe.AddStep("Cook until ready.");
        recipe.AddIngredient("Salt", 1, "g");
        recipe.Publish();
        return recipe;
    }

    private static async Task<string> ExplainAsync(
        ApplicationDbContext dbContext,
        string sql,
        string searchTerm)
    {
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
        command.Parameters.Add(new NpgsqlParameter("searchTerm", NpgsqlDbType.Text)
        {
            Value = searchTerm,
        });

        await using var reader = await command.ExecuteReaderAsync();
        var lines = new List<string>();
        while (await reader.ReadAsync())
        {
            lines.Add(reader.GetString(0));
        }

        return string.Join(Environment.NewLine, lines);
    }

    private sealed record SearchIndexDefinition(string AccessMethod, string Definition);
}
