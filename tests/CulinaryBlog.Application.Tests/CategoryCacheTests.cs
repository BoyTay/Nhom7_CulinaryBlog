using System.Diagnostics.Metrics;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Categories;
using CulinaryBlog.Domain.Categories;
using Microsoft.Extensions.Caching.Memory;

namespace CulinaryBlog.Application.Tests;

public sealed class CategoryCacheTests
{
    [Fact]
    public async Task SecondListCallIsServedFromCacheWithoutQueryingRepository()
    {
        var repository = new CountingRepository();
        repository.Seed(Category.Create("Món chính", "mon-chinh"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetCategoriesQueryHandler(repository, cache);

        var first = await handler.Handle(new GetCategoriesQuery(), default);
        var second = await handler.Handle(new GetCategoriesQuery(), default);

        Assert.Equal(1, repository.ListCalls);
        Assert.Same(first, second);
        Assert.True(cache.TryGetValue(CategoryQueries.CacheKey, out _));
    }

    [Fact]
    public async Task ListRecordsOneMissThenOneHitMetric()
    {
        var results = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == CategoryMetrics.MeterName && instrument.Name == "culinaryblog.category.cache.requests")
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            foreach (var tag in tags)
            {
                if (tag.Key == "result" && tag.Value is string value)
                {
                    lock (results)
                    {
                        results.Add(value);
                    }
                }
            }
        });
        listener.Start();

        using var cache = new MemoryCache(new MemoryCacheOptions());
        var handler = new GetCategoriesQueryHandler(new CountingRepository(), cache);
        await handler.Handle(new GetCategoriesQuery(), default);
        await handler.Handle(new GetCategoriesQuery(), default);

        // Meters are process-wide and other tests also read the list, so assert "at least once".
        lock (results)
        {
            Assert.Contains("miss", results);
            Assert.Contains("hit", results);
        }
    }

    [Fact]
    public async Task UpdateInvalidatesCacheAndNextListShowsNewName()
    {
        var repository = new CountingRepository();
        var category = Category.Create("Món chính", "mon-chinh");
        repository.Seed(category);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var list = new GetCategoriesQueryHandler(repository, cache);

        await list.Handle(new GetCategoriesQuery(), default);
        await new UpdateCategoryCommandHandler(repository, new NoopSession(), cache)
            .Handle(new UpdateCategoryCommand(category.Id, "Món Việt", null, null, 0), default);

        Assert.False(cache.TryGetValue(CategoryQueries.CacheKey, out _));
        var refreshed = await list.Handle(new GetCategoriesQuery(), default);
        Assert.Equal(2, repository.ListCalls);
        Assert.Equal("Món Việt", Assert.Single(refreshed).Name);
    }

    [Fact]
    public async Task DeleteInvalidatesCacheAndNextListOmitsCategory()
    {
        var repository = new CountingRepository();
        var category = Category.Create("Món chính", "mon-chinh");
        repository.Seed(category);
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var list = new GetCategoriesQueryHandler(repository, cache);

        Assert.Single(await list.Handle(new GetCategoriesQuery(), default));
        await new DeleteCategoryCommandHandler(repository, new NoopSession(), cache, TimeProvider.System)
            .Handle(new DeleteCategoryCommand(category.Id), default);

        Assert.False(cache.TryGetValue(CategoryQueries.CacheKey, out _));
        Assert.Empty(await list.Handle(new GetCategoriesQuery(), default));
        Assert.Equal(2, repository.ListCalls);
    }

    [Fact]
    public async Task FailedCreateKeepsCachedListUntouched()
    {
        var repository = new CountingRepository();
        repository.Seed(Category.Create("Món chính", "mon-chinh"));
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var list = new GetCategoriesQueryHandler(repository, cache);
        await list.Handle(new GetCategoriesQuery(), default);

        await Assert.ThrowsAsync<CulinaryBlog.Application.Common.Exceptions.ConflictException>(() =>
            new CreateCategoryCommandHandler(repository, new NoopSession(), cache)
                .Handle(new CreateCategoryCommand("món chính", null, null, 0), default));

        Assert.True(cache.TryGetValue(CategoryQueries.CacheKey, out _));
        await list.Handle(new GetCategoriesQuery(), default);
        Assert.Equal(1, repository.ListCalls);
    }

    private sealed class NoopSession : IDataSession
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class CountingRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];

        public int ListCalls { get; private set; }

        public void Seed(Category category) => _categories.Add(category);

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
            Task.FromResult(0);

        public Task<(IReadOnlyList<CategoryRecipeSummary> Items, int TotalCount)> ListRecipesAsync(
            Guid categoryId, string? authorId, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Task.FromResult<(IReadOnlyList<CategoryRecipeSummary>, int)>(([], 0));

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default)
        {
            ListCalls++;
            return Task.FromResult<IReadOnlyList<Category>>(_categories.Where(category => !category.IsDeleted).ToList());
        }

        public void Add(Category category) => _categories.Add(category);
    }
}
