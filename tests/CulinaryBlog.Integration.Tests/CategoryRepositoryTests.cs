using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Categories;
using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CulinaryBlog.Integration.Tests;

public sealed class CategoryRepositoryTests : IClassFixture<CategoryRepositoryTests.RepositoryFactory>
{
    private readonly RepositoryFactory _factory;

    public CategoryRepositoryTests(RepositoryFactory factory) => _factory = factory;

    [Fact]
    public async Task RecipeVisibilityFollowsFrCat002ForGuestAuthorAndOtherAuthor()
    {
        await using var database = await CategoryTestDatabase.CreateAsync(_factory.Services);
        var category = Category.Create("Món canh", "mon-canh");
        var published = CreateRecipe(category.Id, "Canh chua", "author-1", RecipeStatus.Published);
        var ownDraft = CreateRecipe(category.Id, "Canh bí", "author-1", RecipeStatus.Draft);
        var otherDraft = CreateRecipe(category.Id, "Canh rau", "author-2", RecipeStatus.Draft);
        var archived = CreateRecipe(category.Id, "Canh cũ", "author-1", RecipeStatus.Archived);
        database.DbContext.Categories.Add(category);
        database.DbContext.Recipes.AddRange(published, ownDraft, otherDraft, archived);
        await database.DbContext.SaveChangesAsync();
        var repository = new CategoryRepository(database.DbContext);

        var guest = await repository.ListRecipesAsync(category.Id, null, 1, 12);
        Assert.Equal([published.Id], guest.Items.Select(item => item.Id));
        Assert.Equal(1, guest.TotalCount);

        var author = await repository.ListRecipesAsync(category.Id, "author-1", 1, 12);
        Assert.Equal(2, author.TotalCount);
        Assert.Contains(author.Items, item => item.Id == ownDraft.Id);
        Assert.DoesNotContain(author.Items, item => item.Id == otherDraft.Id);
        Assert.DoesNotContain(author.Items, item => item.Id == archived.Id);

        var stranger = await repository.ListRecipesAsync(category.Id, "author-3", 1, 12);
        Assert.Equal([published.Id], stranger.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task ListRecipesPagesNewestFirstAndReportsTotalCount()
    {
        await using var database = await CategoryTestDatabase.CreateAsync(_factory.Services);
        var category = Category.Create("Món nướng", "mon-nuong");
        var recipes = Enumerable.Range(1, 3)
            .Select(index => CreateRecipe(category.Id, $"Nướng {index}", "author-1", RecipeStatus.Published))
            .ToArray();
        database.DbContext.Categories.Add(category);
        database.DbContext.Recipes.AddRange(recipes);
        await database.DbContext.SaveChangesAsync();
        var repository = new CategoryRepository(database.DbContext);

        var first = await repository.ListRecipesAsync(category.Id, null, 1, 2);
        var second = await repository.ListRecipesAsync(category.Id, null, 2, 2);
        var beyond = await repository.ListRecipesAsync(category.Id, null, 3, 2);

        Assert.Equal(3, first.TotalCount);
        Assert.Equal(2, first.Items.Count);
        Assert.Single(second.Items);
        Assert.Empty(beyond.Items);
        Assert.Equal(3, beyond.TotalCount);
        Assert.Equal(
            recipes.Select(recipe => recipe.Id).Order(),
            first.Items.Concat(second.Items).Select(item => item.Id).Order());
        Assert.True(first.Items[0].CreatedAt >= first.Items[1].CreatedAt);
    }

    [Fact]
    public async Task CountRecipesDistinguishesPublishedFromAllAndIgnoresSoftDeletedRecipes()
    {
        await using var database = await CategoryTestDatabase.CreateAsync(_factory.Services);
        var category = Category.Create("Món chay", "mon-chay");
        var published = CreateRecipe(category.Id, "Đậu hũ", "author-1", RecipeStatus.Published);
        var draft = CreateRecipe(category.Id, "Nấm kho", "author-1", RecipeStatus.Draft);
        var removed = CreateRecipe(category.Id, "Đã xóa", "author-1", RecipeStatus.Published);
        removed.Delete(DateTimeOffset.UtcNow);
        database.DbContext.Categories.Add(category);
        database.DbContext.Recipes.AddRange(published, draft, removed);
        await database.DbContext.SaveChangesAsync();
        var repository = new CategoryRepository(database.DbContext);

        Assert.Equal(1, await repository.CountRecipesAsync(category.Id, true));
        Assert.Equal(2, await repository.CountRecipesAsync(category.Id, false));
    }

    [Fact]
    public async Task DeleteAllowsCategoryWhoseOnlyRecipesAreSoftDeletedAndHidesItFromReads()
    {
        await using var database = await CategoryTestDatabase.CreateAsync(_factory.Services);
        var category = Category.Create("Món lẩu", "mon-lau");
        var removed = CreateRecipe(category.Id, "Lẩu cũ", "author-1", RecipeStatus.Published);
        removed.Delete(DateTimeOffset.UtcNow);
        database.DbContext.Categories.Add(category);
        database.DbContext.Recipes.Add(removed);
        await database.DbContext.SaveChangesAsync();
        var repository = new CategoryRepository(database.DbContext);
        using var cache = new MemoryCache(new MemoryCacheOptions());

        await new DeleteCategoryCommandHandler(
                repository, (IDataSession)database.DbContext, cache, TimeProvider.System)
            .Handle(new DeleteCategoryCommand(category.Id), default);

        Assert.Null(await repository.GetBySlugAsync("mon-lau"));
        Assert.Null(await repository.GetByIdAsync(category.Id));
        Assert.DoesNotContain(await repository.ListAsync(), item => item.Id == category.Id);
    }

    [Fact]
    public async Task DeletedCategoryNameAndSlugStayReservedSoUniqueIndexesCannotBreakCreate()
    {
        await using var database = await CategoryTestDatabase.CreateAsync(_factory.Services);
        var category = Category.Create("Món tráng miệng", "mon-trang-mieng");
        category.Delete(DateTimeOffset.UtcNow);
        database.DbContext.Categories.Add(category);
        await database.DbContext.SaveChangesAsync();
        var repository = new CategoryRepository(database.DbContext);

        Assert.True(await repository.NameExistsAsync("món tráng miệng"));
        Assert.True(await repository.SlugExistsAsync("mon-trang-mieng"));

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var created = await new CreateCategoryCommandHandler(
                repository, (IDataSession)database.DbContext, cache)
            .Handle(new CreateCategoryCommand("Món tráng miệng khác", null, null, 0), default);
        Assert.Equal("mon-trang-mieng-khac", created.Slug);
    }

    [Theory]
    [InlineData("100%")]
    [InlineData("a_b")]
    public async Task NameLookupTreatsLikeWildcardsAsLiteralText(string name)
    {
        await using var database = await CategoryTestDatabase.CreateAsync(_factory.Services);
        database.DbContext.Categories.Add(Category.Create("Abc", "abc"));
        await database.DbContext.SaveChangesAsync();
        var repository = new CategoryRepository(database.DbContext);

        Assert.False(await repository.NameExistsAsync(name));
        Assert.False(await repository.NameExistsAsync("%"));
        Assert.False(await repository.NameExistsAsync("A_c"));
        Assert.True(await repository.NameExistsAsync("ABC"));
    }

    private static Recipe CreateRecipe(Guid categoryId, string title, string authorId, RecipeStatus status)
    {
        var recipe = Recipe.Create(
            title,
            $"{title.Replace(' ', '-')}-{Guid.NewGuid():N}",
            "Công thức kiểm thử danh mục.",
            categoryId,
            authorId,
            5,
            20,
            2,
            RecipeDifficulty.Easy);
        recipe.AddStep("Chuẩn bị và nấu.");
        recipe.AddIngredient("Nguyên liệu", 1, "phần");
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

    private sealed class CategoryTestDatabase : IAsyncDisposable
    {
        private readonly string _adminConnectionString;
        private readonly string _schema;

        private CategoryTestDatabase(ApplicationDbContext dbContext, string adminConnectionString, string schema)
        {
            DbContext = dbContext;
            _adminConnectionString = adminConnectionString;
            _schema = schema;
        }

        public ApplicationDbContext DbContext { get; }

        public static async Task<CategoryTestDatabase> CreateAsync(IServiceProvider services)
        {
            var configuration = services.GetRequiredService<IConfiguration>();
            var configured = configuration.GetConnectionString("Postgres")
                ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
            var adminConnectionString = new NpgsqlConnectionStringBuilder(configured) { Pooling = false }.ConnectionString;
            var schema = $"category_repo_{Guid.NewGuid():N}";

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

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(new NpgsqlConnectionStringBuilder(adminConnectionString) { SearchPath = schema }.ConnectionString)
                .Options;
            var dbContext = new ApplicationDbContext(options);
            try
            {
                await dbContext.Database.MigrateAsync();
                return new CategoryTestDatabase(dbContext, adminConnectionString, schema);
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

    public sealed class RepositoryFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) =>
            builder.UseEnvironment("Development");
    }
}
