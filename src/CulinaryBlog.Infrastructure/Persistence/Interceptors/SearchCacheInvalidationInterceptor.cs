using CulinaryBlog.Domain.Categories;
using CulinaryBlog.Domain.Recipes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CulinaryBlog.Infrastructure.Persistence.Interceptors;

public sealed class SearchCacheInvalidationInterceptor(
    IRecipeSearchCacheInvalidator cacheInvalidator) : SaveChangesInterceptor
{
    private bool _hasSearchableEntityChanges;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureSearchableChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureSearchableChanges(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(
        SaveChangesCompletedEventData eventData,
        int result)
    {
        InvalidateAfterSave(result).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await InvalidateAfterSave(result);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureSearchableChanges(DbContext? context)
    {
        _hasSearchableEntityChanges = context?.ChangeTracker.Entries().Any(entry =>
            (entry.Entity is Recipe or Category) &&
            entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted) == true;
    }

    private Task InvalidateAfterSave(int affectedRows)
    {
        var shouldInvalidate = _hasSearchableEntityChanges && affectedRows > 0;
        _hasSearchableEntityChanges = false;
        return shouldInvalidate
            ? cacheInvalidator.InvalidateAsync()
            : Task.CompletedTask;
    }
}
