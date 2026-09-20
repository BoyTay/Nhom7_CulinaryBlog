namespace CulinaryBlog.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
