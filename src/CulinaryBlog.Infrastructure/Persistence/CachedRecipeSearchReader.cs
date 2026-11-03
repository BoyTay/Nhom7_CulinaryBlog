using System.Diagnostics.Metrics;
using System.Security.Cryptography;
using System.Text.Json;
using CulinaryBlog.Application.Abstractions.Search;
using CulinaryBlog.Application.Common.Pagination;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Persistence;

public sealed class CachedRecipeSearchReader(
    IRecipeSearchReader reader,
    IDistributedCache cache,
    ILogger<CachedRecipeSearchReader> logger) : IRecipeSearchReader
{
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);
    public const string MeterName = "CulinaryBlog.Search";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> CacheRequests =
        Meter.CreateCounter<long>("culinaryblog.search.cache.requests");
    private static readonly Action<ILogger, Exception?> LogCacheReadFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(9501, "RecipeSearchCacheReadFailed"),
            "Recipe search cache read failed; querying PostgreSQL.");
    private static readonly Action<ILogger, Exception?> LogCacheWriteFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(9502, "RecipeSearchCacheWriteFailed"),
            "Recipe search cache write failed; returning PostgreSQL results.");
    private static readonly Action<ILogger, Exception?> LogCacheEntryInvalid =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(9504, "RecipeSearchCacheEntryInvalid"),
            "Recipe search cache entry was invalid; refreshing it from PostgreSQL.");

    public async Task<PagedResult<RecipeSearchResult>> SearchAsync(
        RecipeSearchOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        options = options.Validate();

        string? cacheKey = null;
        try
        {
            var version = await cache.GetStringAsync(
                RecipeSearchCacheInvalidator.VersionCacheKey,
                cancellationToken);
            if (version is null)
            {
                version = Guid.NewGuid().ToString("N");
                await cache.SetStringAsync(
                    RecipeSearchCacheInvalidator.VersionCacheKey,
                    version,
                    cancellationToken);
            }

            cacheKey = CreateCacheKey(version, options);
            var cachedValue = await cache.GetAsync(cacheKey, cancellationToken);
            if (cachedValue is not null)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<SearchCacheEntry>(cachedValue, JsonOptions);
                    if (entry is { Items: not null, TotalCount: >= 0, Page: >= 1, PageSize: >= 1 and <= 50 })
                    {
                        RecordCacheRequest("hit");
                        return PagedResult.Create(
                            entry.Items,
                            entry.TotalCount,
                            entry.Page,
                            entry.PageSize);
                    }

                    LogCacheEntryInvalid(logger, null);
                }
                catch (JsonException exception)
                {
                    LogCacheEntryInvalid(logger, exception);
                }

                await cache.RemoveAsync(cacheKey, cancellationToken);
            }

            RecordCacheRequest("miss");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsCacheFailure(exception))
        {
            cacheKey = null;
            RecordCacheRequest("bypass");
            LogCacheReadFailed(logger, exception);
        }

        var result = await reader.SearchAsync(options, cancellationToken);
        if (cacheKey is null)
        {
            return result;
        }

        try
        {
            var entry = new SearchCacheEntry(
                result.Items.ToArray(),
                result.TotalCount,
                result.Page,
                result.PageSize);
            var serialized = JsonSerializer.SerializeToUtf8Bytes(entry, JsonOptions);
            await cache.SetAsync(
                cacheKey,
                serialized,
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheDuration },
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsCacheFailure(exception))
        {
            LogCacheWriteFailed(logger, exception);
        }

        return result;
    }

    private static string CreateCacheKey(string version, RecipeSearchOptions options)
    {
        var optionsJson = JsonSerializer.SerializeToUtf8Bytes(options, JsonOptions);
        var digest = Convert.ToHexString(SHA256.HashData(optionsJson));
        return $"search:recipes:{version}:{digest}";
    }

    private static bool IsCacheFailure(Exception exception) =>
        exception is RedisException or TimeoutException or IOException or InvalidOperationException or JsonException;

    private static void RecordCacheRequest(string result) =>
        CacheRequests.Add(1, new KeyValuePair<string, object?>("result", result));

    private sealed record SearchCacheEntry(
        RecipeSearchResult[] Items,
        int TotalCount,
        int Page,
        int PageSize);
}
