namespace CulinaryBlog.Domain.Common;

public abstract class AggregateRoot(Guid id) : Entity(id)
{
    public DateTimeOffset CreatedAt { get; protected set; }

    public DateTimeOffset? UpdatedAt { get; protected set; }

    public bool IsDeleted { get; protected set; }

    public uint Version { get; private set; }

    public virtual void Delete(DateTimeOffset deletedAt)
    {
        IsDeleted = true;
        UpdatedAt = deletedAt;
    }
}
