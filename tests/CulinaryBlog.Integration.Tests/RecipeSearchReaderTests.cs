using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CulinaryBlog.Integration.Tests;

public sealed class RecipeSearchReaderTests : IClassFixture<RecipeSearchReaderTests.SearchFactory>
{
    private readonly SearchFactory _factory;

    public RecipeSearchReaderTests(SearchFactory factory) => _factory = factory;

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
    }

    [Fact]
    public async Task SearchOrdersByRelevanceAndScoresTitleMatches()
    {
        const string term = "readercheck";
        var recipes = new[]
        {
            CreateRecipe("readercheck", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published),
            CreateRecipe("readercheck prefix", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published),
            CreateRecipe("title includes readercheck", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published),
            CreateRecipe("description match", Guid.NewGuid(), RecipeDifficulty.Easy, 10, 1, RecipeStatus.Published,
                description: "This mentions readercheck."),
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

        Assert.Equal(recipes.Select(recipe => recipe.Id), result.Items.Select(item => item.Id));
        Assert.Equal(new double?[] { 1, 0.8, 0.6, 0.4 }, result.Items.Select(item => item.RelevanceScore));
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
                command.CommandText = $"CREATE SCHEMA \"{schema}\"";
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
                await dbContext.Database.EnsureCreatedAsync();
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
