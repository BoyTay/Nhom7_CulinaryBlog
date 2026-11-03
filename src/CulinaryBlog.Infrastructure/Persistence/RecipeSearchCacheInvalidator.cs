using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace CulinaryBlog.Infrastructure.Persistence;

public interface IRecipeSearchCacheInvalidator
{
    Task InvalidateAsync(CancellationToken cancellationToken = default);
}

public sealed class RecipeSearchCacheInvalidator(
    IDistributedCache cache,
    ILogger<RecipeSearchCacheInvalidator> logger) : IRecipeSearchCacheInvalidator
{
    internal const string VersionCacheKey = "search:recipes:version";

    private static readonly Action<ILogger, Exception?> LogCacheInvalidationFailed =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(9503, "RecipeSearchCacheInvalidationFailed"),
            "Recipe search cache invalidation failed; cached results may remain visible until their one-minute expiration.");

    public async Task InvalidateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await cache.SetStringAsync(
                VersionCacheKey,
                Guid.NewGuid().ToString("N"),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (
            exception is RedisException or TimeoutException or IOException or InvalidOperationException)
        {
            LogCacheInvalidationFailed(logger, exception);
        }
    }
}
