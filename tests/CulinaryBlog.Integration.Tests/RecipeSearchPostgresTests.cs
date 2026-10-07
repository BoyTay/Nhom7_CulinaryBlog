using System.Net;
using System.Text.Json;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NpgsqlTypes;
using Xunit.Abstractions;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeSearchPostgresTests : IClassFixture<TestWebApplicationFactory>
{
    private static readonly string[] SearchIndexNames =
    [
        "IX_Recipes_SearchVector",
        "IX_Recipes_Title_Trgm",
    ];

    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;

    public RecipeSearchPostgresTests(TestWebApplicationFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _output = output;
    }

    [Fact]
    public async Task SearchEndpointUsesVietnameseFtsAndTrigramFallbackAndKeepsVisibilityRules()
    {
        var categoryId = Guid.NewGuid();
        var token = Guid.NewGuid().ToString("N");
        var published = CreatePublishedRecipe(
            $"Phở bò Huế {token}",
            "A Vietnamese beef noodle soup.",
            categoryId);
        var draft = Recipe.Create(
            $"Phở bò Huế draft {token}",
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
        var descriptionMatch = CreatePublishedRecipe(
            $"Món nước {token}",
            $"Phở bò Huế with a longer description {token}.",
            categoryId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            dbContext.Recipes.AddRange(published, draft, deleted, fuzzyMatch, descriptionMatch);
            await dbContext.SaveChangesAsync();
        }

        var unaccentedResults = await SearchAsync("pho bo hu", categoryId);
        Assert.Contains(published.Id, unaccentedResults);
        Assert.DoesNotContain(draft.Id, unaccentedResults);
        Assert.DoesNotContain(deleted.Id, unaccentedResults);

        var rankedResults = await SearchAsync("pho bo hu", categoryId);
        var rankedIds = rankedResults.ToList();
        var titleMatchIndex = rankedIds.IndexOf(published.Id);
        var descriptionMatchIndex = rankedIds.IndexOf(descriptionMatch.Id);
        Assert.True(
            titleMatchIndex >= 0 && descriptionMatchIndex >= 0 &&
            titleMatchIndex < descriptionMatchIndex);

        var trigramResults = await SearchAsync("mangoo", categoryId);
        Assert.Contains(fuzzyMatch.Id, trigramResults);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var updatedTitle = $"Phở gà {token}";
            await dbContext.Database.ExecuteSqlInterpolatedAsync(
                $"UPDATE \"Recipes\" SET \"Title\" = {updatedTitle} WHERE \"Id\" = {published.Id}");
        }

        var oldTitleResults = await SearchAsync("pho bo hue", categoryId);
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
        await SeedExplainRecipesAsync(dbContext);
        await dbContext.Database.ExecuteSqlRawAsync("ANALYZE \"Recipes\"");
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

        await dbContext.Database.ExecuteSqlRawAsync("SET LOCAL enable_indexscan = off");
        await dbContext.Database.ExecuteSqlRawAsync("SET LOCAL enable_bitmapscan = off");
        var legacyPlan = await ExplainAsync(
            dbContext,
            """
            EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, TIMING OFF)
            SELECT r."Id"
            FROM "Recipes" AS r
            WHERE r."Status" = 'Published'
              AND r."IsDeleted" = FALSE
              AND (r."Title" ILIKE '%' || @searchTerm || '%')
            """,
            "needleham");

        await dbContext.Database.ExecuteSqlRawAsync("RESET enable_indexscan");
        await dbContext.Database.ExecuteSqlRawAsync("RESET enable_bitmapscan");
        var fullTextPlan = await ExplainAsync(
            dbContext,
            """
            EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, TIMING OFF)
            SELECT r."Id"
            FROM "Recipes" AS r
            WHERE r."Status" = 'Published'
              AND r."IsDeleted" = FALSE
              AND r."SearchVector" @@ to_tsquery(
                  'culinary_vietnamese',
                  '''needleha'':*')
            """,
            "needleham");
        var trigramPlan = await ExplainAsync(
            dbContext,
            """
            EXPLAIN (ANALYZE, BUFFERS, COSTS OFF, TIMING OFF)
            SELECT r."Id"
            FROM "Recipes" AS r
            WHERE r."Status" = 'Published'
              AND r."IsDeleted" = FALSE
              AND r."Title" % @searchTerm
            """,
            "needlehamx");

        _output.WriteLine("Before FTS (legacy ILIKE):{0}{1}", Environment.NewLine, legacyPlan);
        _output.WriteLine("After FTS (prefix tsquery):{0}{1}", Environment.NewLine, fullTextPlan);
        _output.WriteLine("After trigram fallback:{0}{1}", Environment.NewLine, trigramPlan);

        Assert.Contains("Seq Scan", legacyPlan, StringComparison.Ordinal);
        Assert.Contains("IX_Recipes_SearchVector", fullTextPlan, StringComparison.Ordinal);
        Assert.Contains("IX_Recipes_Title_Trgm", trigramPlan, StringComparison.Ordinal);
        Assert.Contains("Execution Time:", legacyPlan, StringComparison.Ordinal);
        Assert.Contains("Execution Time:", fullTextPlan, StringComparison.Ordinal);
    }

    private static Task<int> SeedExplainRecipesAsync(ApplicationDbContext dbContext)
    {
        var categoryId = Guid.NewGuid();
        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "Recipes" (
                "Id",
                "Title",
                "Slug",
                "Description",
                "CategoryId",
                "AuthorId",
                "PrepTimeMinutes",
                "CookTimeMinutes",
                "Servings",
                "Difficulty",
                "Status",
                "CreatedAt",
                "UpdatedAt",
                "IsDeleted",
                "PublishedAt")
            SELECT md5('search-explain-' || sample::text)::uuid,
                   CASE WHEN sample = 1 THEN 'needleham'
                        ELSE 'ordinary recipe ' || sample::text END,
                   'search-explain-' || sample::text,
                   'A recipe for the EXPLAIN ANALYZE integration test.',
                   {categoryId},
                   'search-explain-test',
                   5,
                   15,
                   2,
                   'Easy',
                   'Published',
                   now() - sample * interval '1 second',
                   NULL,
                   FALSE,
                   now() - sample * interval '1 second'
            FROM generate_series(1, 10000) AS samples(sample)
            """);
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
            Value = SearchIndexNames,
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
