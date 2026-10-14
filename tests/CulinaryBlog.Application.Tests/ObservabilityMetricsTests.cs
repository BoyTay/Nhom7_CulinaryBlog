using System.Diagnostics.Metrics;
using CulinaryBlog.Application.Abstractions.Persistence;
using CulinaryBlog.Application.Categories;
using CulinaryBlog.Application.Common.Behaviors;
using CulinaryBlog.Domain.Categories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace CulinaryBlog.Application.Tests;

public sealed class ObservabilityMetricsTests
{
    [Fact]
    public async Task CategoryHandlersRecordCreatedUpdatedAndDeletedCounters()
    {
        var counts = new Dictionary<string, long>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == CategoryMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<long>((instrument, value, _, _) =>
        {
            lock (counts)
            {
                counts[instrument.Name] = counts.GetValueOrDefault(instrument.Name) + value;
            }
        });
        listener.Start();

        var repository = new MetricsRepository();
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var session = new MetricsSession();

        var created = await new CreateCategoryCommandHandler(repository, session, cache)
            .Handle(new CreateCategoryCommand("Món nướng", null, null, 0), default);
        await new UpdateCategoryCommandHandler(repository, session, cache)
            .Handle(new UpdateCategoryCommand(created.Id, "Món nướng mới", null, null, 1), default);
        await new DeleteCategoryCommandHandler(repository, session, cache, TimeProvider.System)
            .Handle(new DeleteCategoryCommand(created.Id), default);

        // Meters are process-wide and other tests run handlers too, so assert "at least once".
        lock (counts)
        {
            Assert.True(counts.GetValueOrDefault("culinaryblog.category.created") >= 1);
            Assert.True(counts.GetValueOrDefault("culinaryblog.category.updated") >= 1);
            Assert.True(counts.GetValueOrDefault("culinaryblog.category.deleted") >= 1);
        }
    }

    [Fact]
    public async Task RequestBehaviorRecordsDurationAndErrorsWithoutPayload()
    {
        const string requestName = nameof(MetricsProbe);
        var durations = new List<string>();
        var errors = new List<string?>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, meterListener) =>
        {
            if (instrument.Meter.Name == RequestMetrics.MeterName)
            {
                meterListener.EnableMeasurementEvents(instrument);
            }
        };
        listener.SetMeasurementEventCallback<double>((_, _, tags, _) =>
        {
            if (GetTag(tags, "request") == requestName)
            {
                lock (durations)
                {
                    durations.Add(GetTag(tags, "outcome") ?? string.Empty);
                }
            }
        });
        listener.SetMeasurementEventCallback<long>((_, _, tags, _) =>
        {
            if (GetTag(tags, "request") == requestName)
            {
                lock (errors)
                {
                    errors.Add(GetTag(tags, "exception"));
                }
            }
        });
        listener.Start();

        var behavior = new RequestTimingBehavior<MetricsProbe, string>(
            TimeProvider.System, NullLogger<RequestTimingBehavior<MetricsProbe, string>>.Instance);
        await behavior.Handle(new MetricsProbe("secret-token"), _ => Task.FromResult("ok"), default);
        await Assert.ThrowsAsync<InvalidOperationException>(() => behavior.Handle(
            new MetricsProbe("secret-token"), _ => throw new InvalidOperationException("boom secret-token"), default));

        lock (durations)
        {
            Assert.Contains("success", durations);
            Assert.Contains("error", durations);
        }

        lock (errors)
        {
            Assert.Equal(["InvalidOperationException"], errors);
        }
    }

    private static string? GetTag(ReadOnlySpan<KeyValuePair<string, object?>> tags, string key)
    {
        foreach (var tag in tags)
        {
            if (tag.Key == key)
            {
                return tag.Value?.ToString();
            }
        }

        return null;
    }

    private sealed record MetricsProbe(string Secret);

    private sealed class MetricsSession : IDataSession
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }

    private sealed class MetricsRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];

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

        public Task<IReadOnlyList<Category>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Category>>(_categories.Where(category => !category.IsDeleted).ToList());

        public void Add(Category category) => _categories.Add(category);
    }
}
