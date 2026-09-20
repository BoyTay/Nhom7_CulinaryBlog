namespace CulinaryBlog.Application.Abstractions.Persistence;

public interface IDataSession
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
