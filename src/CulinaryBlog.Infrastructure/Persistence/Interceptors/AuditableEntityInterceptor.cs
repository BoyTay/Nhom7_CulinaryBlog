using CulinaryBlog.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace CulinaryBlog.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateTrackedEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateTrackedEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateTrackedEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        foreach (var entry in context.ChangeTracker.Entries<AggregateRoot>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(entity => entity.CreatedAt).CurrentValue = now;
                    break;
                case EntityState.Modified:
                    entry.Property(entity => entity.UpdatedAt).CurrentValue = now;
                    break;
                case EntityState.Deleted:
                    entry.State = EntityState.Modified;
                    entry.Property(entity => entity.IsDeleted).CurrentValue = true;
                    entry.Property(entity => entity.UpdatedAt).CurrentValue = now;
                    break;
            }
        }
    }
}
