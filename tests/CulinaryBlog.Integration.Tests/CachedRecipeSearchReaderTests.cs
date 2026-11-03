using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.Common.Pagination;
using CulinaryBlog.Infrastructure;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CulinaryBlog.Integration.Tests;

public sealed class CachedRecipeSearchReaderTests
{
    [Fact]
    public async Task RepeatedNormalizedSearchIsServedFromCacheForOneMinute()
    {
        var inner = new RecordingSearchReader();
        var cache = CreateCache();
        var reader = CreateReader(inner, cache);
        var options = new RecipeSearchOptions { SearchTerm = " soup " };

        var first = await reader.SearchAsync(options);
        var second = await reader.SearchAsync(options with { SearchTerm = "soup" });

        Assert.Equal(1, inner.SearchCount);
        Assert.Equal(first.Items, second.Items);
        Assert.Equal(TimeSpan.FromMinutes(1), CachedRecipeSearchReader.CacheDuration);
    }

    [Fact]
    public async Task ChangingCacheVersionRefreshesSearchResults()
    {
        var inner = new RecordingSearchReader();
        var cache = CreateCache();
        var reader = CreateReader(inner, cache);
        var invalidator = new RecipeSearchCacheInvalidator(
            cache,
            NullLogger<RecipeSearchCacheInvalidator>.Instance);
        var options = new RecipeSearchOptions { SearchTerm = "soup" };

        var initial = await reader.SearchAsync(options);
        inner.Result = CreateResult("Updated soup");
        await invalidator.InvalidateAsync();
        var refreshed = await reader.SearchAsync(options);

        Assert.Equal(2, inner.SearchCount);
        Assert.NotEqual(Assert.Single(initial.Items).Title, Assert.Single(refreshed.Items).Title);
        Assert.Equal("Updated soup", Assert.Single(refreshed.Items).Title);
    }

    [Fact]
    public async Task DifferentFiltersUseDifferentCacheEntries()
    {
        var inner = new RecordingSearchReader();
        var reader = CreateReader(inner, CreateCache());

        await reader.SearchAsync(new RecipeSearchOptions { SearchTerm = "soup" });
        await reader.SearchAsync(new RecipeSearchOptions
        {
            SearchTerm = "soup",
            CategoryId = Guid.NewGuid(),
        });

        Assert.Equal(2, inner.SearchCount);
    }

    [Fact]
    public async Task CacheFailureFallsBackToSearchReader()
    {
        var inner = new RecordingSearchReader();
        var reader = CreateReader(inner, new UnavailableDistributedCache());

        var result = await reader.SearchAsync(new RecipeSearchOptions { SearchTerm = "soup" });

        Assert.Equal(1, inner.SearchCount);
        Assert.Equal("Tomato soup", Assert.Single(result.Items).Title);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("localhost:6379,abortConnect=false", true)]
    public void InfrastructureRegistersTheConfiguredDistributedSearchCache(
        string? redisConnection,
        bool useRedis)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] =
                    "Host=localhost;Port=5432;Database=culinary_blog;Username=culinary",
                ["ConnectionStrings:Redis"] = redisConnection,
                ["Hangfire:Enabled"] = "false",
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<IDistributedCache>();

        if (useRedis)
        {
            Assert.False(cache is MemoryDistributedCache);
        }
        else
        {
            Assert.IsType<MemoryDistributedCache>(cache);
        }

        Assert.IsType<CachedRecipeSearchReader>(
            scope.ServiceProvider.GetRequiredService<IRecipeSearchReader>());
    }

    private static CachedRecipeSearchReader CreateReader(
        IRecipeSearchReader inner,
        IDistributedCache cache) =>
        new(inner, cache, NullLogger<CachedRecipeSearchReader>.Instance);

    private static MemoryDistributedCache CreateCache() =>
        new(Options.Create(new MemoryDistributedCacheOptions()));

    private static PagedResult<RecipeSearchResult> CreateResult(string title) =>
        PagedResult.Create(
            new[]
            {
                new RecipeSearchResult(
                    Guid.Parse("df3a33c8-6e8f-46a9-9eb3-13e7b4071434"),
                    "tomato-soup",
                    title,
                    "A simple soup recipe.",
                    Guid.Parse("c6ff4415-69bb-49a6-91a6-f933ab022184"),
                    "Easy",
                    20,
                    new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero)),
            },
            1,
            1,
            12);

    private sealed class RecordingSearchReader : IRecipeSearchReader
    {
        public int SearchCount { get; private set; }

        public PagedResult<RecipeSearchResult> Result { get; set; } = CreateResult("Tomato soup");

        public Task<PagedResult<RecipeSearchResult>> SearchAsync(
            RecipeSearchOptions options,
            CancellationToken cancellationToken = default)
        {
            SearchCount++;
            return Task.FromResult(Result);
        }
    }

    private sealed class UnavailableDistributedCache : IDistributedCache
    {
        public byte[]? Get(string key) => throw new IOException("Cache unavailable.");

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromException<byte[]?>(new IOException("Cache unavailable."));

        public void Refresh(string key) => throw new IOException("Cache unavailable.");

        public Task RefreshAsync(string key, CancellationToken token = default) =>
            Task.FromException(new IOException("Cache unavailable."));

        public void Remove(string key) => throw new IOException("Cache unavailable.");

        public Task RemoveAsync(string key, CancellationToken token = default) =>
            Task.FromException(new IOException("Cache unavailable."));

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            throw new IOException("Cache unavailable.");

        public Task SetAsync(
            string key,
            byte[] value,
            DistributedCacheEntryOptions options,
            CancellationToken token = default) =>
            Task.FromException(new IOException("Cache unavailable."));
    }
}
