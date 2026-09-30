using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Categories;
using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Categories;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Tests;

public sealed class CategoryCoreTests
{
    [Fact]
    public async Task CreateUsesUniqueVietnameseSlugAndRejectsDuplicateName()
    {
        var repository = new FakeCategoryRepository();
        var session = new FakeSession();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new CreateCategoryCommandHandler(repository, session, cache);

        var first = await handler.Handle(new CreateCategoryCommand("Món chính", null, null, 0), default);
        Assert.Equal("mon-chinh", first.Slug);

        repository.Add(Category.Create("Món chính khác", "mon-chinh-2"));
        var second = await handler.Handle(new CreateCategoryCommand("Món Chính!", null, null, 0), default);
        Assert.Equal("mon-chinh-3", second.Slug);
        Assert.Equal(2, session.SaveCount);

        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            handler.Handle(new CreateCategoryCommand("món chính", null, null, 0), default));
        Assert.Equal("CATEGORY_NAME_EXISTS", error.Code);
    }

    [Fact]
    public async Task UpdatePreservesSlugAndSoftDeleteRefusesCategoryWithRecipes()
    {
        var repository = new FakeCategoryRepository { RecipeCount = 2 };
        var category = Category.Create("Món chính", "mon-chinh");
        repository.Add(category);
        var session = new FakeSession();
        using var cache = new MemoryCache(new MemoryCacheOptions());

        var updated = await new UpdateCategoryCommandHandler(repository, session, cache)
            .Handle(new UpdateCategoryCommand(category.Id, "Món Việt", null, null, 1), default);
        Assert.Equal("mon-chinh", updated.Slug);
        Assert.Equal("Món Việt", updated.Name);

        var delete = new DeleteCategoryCommandHandler(repository, session, cache, TimeProvider.System);
        var error = await Assert.ThrowsAsync<ConflictException>(() =>
            delete.Handle(new DeleteCategoryCommand(category.Id), default));
        Assert.Equal("CATEGORY_DELETE_HAS_RECIPES", error.Code);
        Assert.False(category.IsDeleted);

        repository.RecipeCount = 0;
        await delete.Handle(new DeleteCategoryCommand(category.Id), default);
        Assert.True(category.IsDeleted);
        Assert.Equal(2, session.SaveCount);
    }

    [Theory]
    [InlineData("<b>Bad</b>")]
    [InlineData("A")]
    [InlineData("   ")]
    public void CreateValidatorRejectsInvalidNames(string name)
    {
        var result = new CreateCategoryCommandValidator().Validate(new CreateCategoryCommand(name, null, null, 0));
        Assert.False(result.IsValid);
    }

    [Fact]
    public void DetailValidatorRejectsInvalidPagination()
    {
        var result = new GetCategoryBySlugQueryValidator().Validate(new GetCategoryBySlugQuery("mon-chinh", 0, 51));
        Assert.False(result.IsValid);
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task CategoryListCacheIsInvalidatedAfterCreate()
    {
        var repository = new FakeCategoryRepository();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var listHandler = new GetCategoriesQueryHandler(repository, cache);
        var createHandler = new CreateCategoryCommandHandler(repository, new FakeSession(), cache);

        Assert.Empty(await listHandler.Handle(new GetCategoriesQuery(), default));
        await createHandler.Handle(new CreateCategoryCommand("Món chính", null, null, 0), default);
        var list = await listHandler.Handle(new GetCategoriesQuery(), default);
        Assert.Single(list);
    }

    private sealed class FakeSession : IDataSession
    {
        public int SaveCount { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];

        public int RecipeCount { get; set; }

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.SingleOrDefault(category => category.Id == id && !category.IsDeleted));

        public Task<Category?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.SingleOrDefault(category => category.Slug == slug && !category.IsDeleted));

        public Task<bool> NameExistsAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.Any(category => category.Id != excludeId &&
                string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase)));

        public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult(_categories.Any(category => category.Slug == slug));

        public Task<int> CountRecipesAsync(Guid categoryId, bool publishedOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult(RecipeCount);

        public Task<(IReadOnlyList<CategoryRecipeSummary> Items, int TotalCount)> ListRecipesAsync(
            Guid categoryId, string? authorId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CategoryRecipeSummary>, int)>(([], 0));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_categories.Where(category => !category.IsDeleted).ToList());

        public void Add(Category category) => _categories.Add(category);
    }
}
