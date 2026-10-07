using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit.Abstractions;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeSearchReaderTests : IClassFixture<RecipeSearchReaderTests.SearchFactory>
{
    private readonly SearchFactory _factory;
    private readonly ITestOutputHelper _output;

    public RecipeSearchReaderTests(SearchFactory factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task SearchFiltersPublishedRecipesAndReturnsStablePagedResults()
    {
        var categoryId = Guid.NewGuid();
        var otherCategoryId = Guid.NewGuid();
        var recipes = new[]
        {
            CreateRecipe("soup alpha", categoryId, RecipeDifficulty.Easy, 20, 2, RecipeStatus.Published),
            CreateRecipe("soup beta", categoryId, RecipeDifficulty.Easy, 40, 4, RecipeStatus.Published),
            CreateRecipe("soup gamma", categoryId, RecipeDifficulty.Hard, 20, 2, RecipeStatus.Published),
            CreateRecipe("soup outside", otherCategoryId, RecipeDifficulty.Easy, 20, 2, RecipeStatus.Published),
            CreateRecipe("soup draft", categoryId, RecipeDifficulty.Easy, 20, 2, RecipeStatus.Draft),
            CreateRecipe("soup archived", categoryId, RecipeDifficulty.Easy, 20, 2, RecipeStatus.Archived),
        };

        await using var database = await SearchTestDatabase.CreateAsync(_factory.Services);
        var dbContext = database.DbContext;
        dbContext.Recipes.AddRange(recipes);
        await dbContext.SaveChangesAsync();

        var reader = new RecipeSearchReader(dbContext);
        var filtered = await reader.SearchAsync(new RecipeSearchOptions
        {
            SearchTerm = "soup",
            CategoryId = categoryId,
            Difficulty = "Easy",
            MaxCookTime = 45,
            MinServings = 3,
        });

        Assert.Equal(new[] { recipes[1].Id }, filtered.Items.Select(item => item.Id));
        Assert.Equal(1, filtered.TotalCount);

        var page = await reader.SearchAsync(new RecipeSearchOptions
        {
            SearchTerm = "soup",
            CategoryId = categoryId,
            Sort = "title",
            Page = 2,
            PageSize = 1,
        });

        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Equal(new[] { recipes[1].Id }, page.Items.Select(item => item.Id));

        var browse = await reader.SearchAsync(new RecipeSearchOptions
        {
            CategoryId = categoryId,
            Sort = "createdAt",
        });

        Assert.Equal(3, browse.TotalCount);
        Assert.All(browse.Items, item => Assert.Null(item.RelevanceScore));
    }

    [Fact]
    public async Task SearchOrdersByRelevanceAndScoresTitleMatches()
    {
        const string term = "readercheck soup";
        var recipes = new[]
        {
            CreateRecipe("readercheck soup soup soup", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published),
            CreateRecipe("readercheck soup prefix", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published),
            CreateRecipe("title includes readercheck soup", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published),
            CreateRecipe("description match", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published,
                description: "This mentions readercheck soup."),
        };

        await using var database = await SearchTestDatabase.CreateAsync(_factory.Services);
        var dbContext = database.DbContext;
        dbContext.Recipes.AddRange(recipes);
        await dbContext.SaveChangesAsync();

        var reader = new RecipeSearchReader(dbContext);
        var result = await reader.SearchAsync(new RecipeSearchOptions
        {
            SearchTerm = term,
            Sort = "-relevance",
            PageSize = 10,
        });

        Assert.Equal(recipes.Length, result.TotalCount);
        Assert.Equal(recipes[0].Id, result.Items[0].Id);
        Assert.Equal(recipes[3].Id, result.Items[^1].Id);
        Assert.All(result.Items, item => Assert.True(item.RelevanceScore > 0));
        Assert.Equal(
            result.Items.Select(item => item.RelevanceScore).OrderByDescending(score => score),
            result.Items.Select(item => item.RelevanceScore));
    }

    [Fact]
    public async Task SearchMatchesVietnameseWithOrWithoutDiacriticsAndPrefix()
    {
        var published = CreateRecipe(
            "Bánh phở bò",
            Guid.NewGuid(),
            RecipeDifficulty.Easy,
            30,
            2,
            RecipeStatus.Published);
        var draft = CreateRecipe(
            "Bánh phở nháp",
            Guid.NewGuid(),
            RecipeDifficulty.Easy,
            30,
            2,
            RecipeStatus.Draft);
        var deleted = CreateRecipe(
            "Bánh phở đã xóa",
            Guid.NewGuid(),
            RecipeDifficulty.Easy,
            30,
            2,
            RecipeStatus.Published);
        deleted.Delete(DateTimeOffset.UtcNow);

        await using var database = await SearchTestDatabase.CreateAsync(_factory.Services);
        var dbContext = database.DbContext;
        dbContext.Recipes.AddRange(published, draft, deleted);
        await dbContext.SaveChangesAsync();

        var reader = new RecipeSearchReader(dbContext);
        foreach (var term in new[] { "banh pho", "bánh phở", "pho bo", "ban" })
        {
            var result = await reader.SearchAsync(new RecipeSearchOptions
            {
                SearchTerm = term,
                PageSize = 10,
            });

            Assert.Contains(result.Items, item => item.Id == published.Id);
            Assert.DoesNotContain(result.Items, item => item.Id == draft.Id);
            Assert.DoesNotContain(result.Items, item => item.Id == deleted.Id);
        }
    }

    [Fact]
    public async Task SearchVectorTriggerReindexesUpdatedRecipeText()
    {
        var recipe = CreateRecipe(
            "old search phrase",
            Guid.NewGuid(),
            RecipeDifficulty.Easy,
            20,
            2,
            RecipeStatus.Published);

        await using var database = await SearchTestDatabase.CreateAsync(_factory.Services);
        var dbContext = database.DbContext;
        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync();

        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""UPDATE "Recipes" SET "Title" = {"updated search phrase"} WHERE "Id" = {recipe.Id}""");

        var reader = new RecipeSearchReader(dbContext);
        var oldText = await reader.SearchAsync(new RecipeSearchOptions { SearchTerm = "old search" });
        var updatedText = await reader.SearchAsync(new RecipeSearchOptions { SearchTerm = "updated search" });

        Assert.DoesNotContain(oldText.Items, item => item.Id == recipe.Id);
        Assert.Contains(updatedText.Items, item => item.Id == recipe.Id);
    }

    [Fact]
    public async Task SearchUsesGinIndexForSelectiveFullTextQuery()
    {
        await using var database = await SearchTestDatabase.CreateAsync(_factory.Services);
        var dbContext = database.DbContext;
        await dbContext.Database.ExecuteSqlRawAsync(
            """
            INSERT INTO "Recipes" (
                "Id", "Title", "Slug", "Description", "CategoryId", "AuthorId",
                "PrepTimeMinutes", "CookTimeMinutes", "Servings", "Difficulty",
                "Status", "CreatedAt", "UpdatedAt", "IsDeleted", "PublishedAt")
            SELECT
                gen_random_uuid(),
                'ordinary recipe ' || series,
                'ordinary-recipe-' || series,
                'A seeded recipe without the rare search token.',
                gen_random_uuid(),
                'search-plan-test',
                5,
                15,
                2,
                'Easy',
                'Published',
                now(),
                NULL,
                FALSE,
                now()
            FROM generate_series(1, 10000) AS series
            """);

        var targetRecipe = CreateRecipe(
            "uniqueneedle target",
            Guid.NewGuid(),
            RecipeDifficulty.Easy,
            15,
            2,
            RecipeStatus.Published);
        dbContext.Recipes.Add(targetRecipe);
        await dbContext.SaveChangesAsync();
        await dbContext.Database.ExecuteSqlRawAsync("ANALYZE \"Recipes\"");

        await dbContext.Database.ExecuteSqlRawAsync("DROP INDEX \"IX_Recipes_SearchVector\"");
        await dbContext.Database.ExecuteSqlRawAsync("ANALYZE \"Recipes\"");
        var withoutIndexPlan = await GetExplainPlanAsync(dbContext);
        Assert.Contains("Seq Scan on \"Recipes\"", withoutIndexPlan, StringComparison.Ordinal);

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            CREATE INDEX "IX_Recipes_SearchVector"
                ON "Recipes"
                USING GIN ("SearchVector")
            """);
        await dbContext.Database.ExecuteSqlRawAsync("ANALYZE \"Recipes\"");
        var withIndexPlan = await GetExplainPlanAsync(dbContext);
        Assert.Contains("IX_Recipes_SearchVector", withIndexPlan, StringComparison.Ordinal);
        _output.WriteLine("EXPLAIN ANALYZE without GIN index:");
        _output.WriteLine(withoutIndexPlan);
        _output.WriteLine("EXPLAIN ANALYZE with GIN index:");
        _output.WriteLine(withIndexPlan);

        var reader = new RecipeSearchReader(dbContext);
        var results = await reader.SearchAsync(new RecipeSearchOptions
        {
            SearchTerm = "uniqueneedle",
            PageSize = 10,
        });

        Assert.Equal(new[] { targetRecipe.Id }, results.Items.Select(item => item.Id));
    }

    private static async Task<string> GetExplainPlanAsync(ApplicationDbContext dbContext)
    {
        var connection = dbContext.Database.GetDbConnection();
        await dbContext.Database.OpenConnectionAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                EXPLAIN (ANALYZE, BUFFERS, FORMAT TEXT)
                SELECT "Id"
                FROM "Recipes"
                WHERE "Status" = 'Published'
                    AND "IsDeleted" = FALSE
                    AND "SearchVector" @@ to_tsquery(
                        'simple',
                        public.unaccent('public.unaccent', 'uniqueneedle:*'))
                """;

            await using var planReader = await command.ExecuteReaderAsync();
            var planLines = new List<string>();
            while (await planReader.ReadAsync())
            {
                planLines.Add(planReader.GetString(0));
            }

            return string.Join(Environment.NewLine, planLines);
        }
        finally
        {
            await dbContext.Database.CloseConnectionAsync();
        }
    }

    private static Recipe CreateRecipe(
        string title,
        Guid categoryId,
        RecipeDifficulty difficulty,
        int cookTime,
        int servings,
        RecipeStatus status,
        string description = "A reader integration recipe.")
    {
        var recipe = Recipe.Create(
            title,
            $"{title.Replace(' ', '-')}-{Guid.NewGuid():N}",
            description,
            categoryId,
            "search-reader-test-author",
            5,
            cookTime,
            servings,
            difficulty);
        recipe.AddStep("Prepare and serve.");
        recipe.AddIngredient("Test ingredient", 1, "unit");

        if (status == RecipeStatus.Published)
        {
            recipe.Publish();
        }
        else if (status == RecipeStatus.Archived)
        {
            recipe.Archive();
        }

        return recipe;
    }

    private sealed class SearchTestDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        private readonly string _schema;

        private SearchTestDatabase(
            ApplicationDbContext dbContext,
            string adminConnectionString,
            string schema)
        {
            DbContext = dbContext;
            _adminConnectionString = adminConnectionString;
            _schema = schema;
        }

        public ApplicationDbContext DbContext { get; }

        public static async Task<SearchTestDatabase> CreateAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var configuredConnectionString = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
            var adminConnectionString = new NpgsqlConnectionStringBuilder(configuredConnectionString)
            {
                Pooling = false,
            }.ConnectionString;
            var schema = $"search_reader_{Guid.NewGuid():N}";

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
                return new SearchTestDatabase(dbContext, adminConnectionString, schema);
            }
            catch
            {
                await dbContext.DisposeAsync();
                await DropSchemaAsync(adminConnectionString, schema);
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DbContext.DisposeAsync();
            await DropSchemaAsync(_adminConnectionString, _schema);
        }

        private static async Task DropSchemaAsync(string connectionString, string schema)
        {
            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"DROP SCHEMA \"{schema}\" CASCADE";
            await command.ExecuteNonQueryAsync();
        }
    }

    public sealed class SearchFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment("Development");
    }
}
