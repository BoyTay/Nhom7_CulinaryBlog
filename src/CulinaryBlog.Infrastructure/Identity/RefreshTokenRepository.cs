namespace CulinaryBlog.Infrastructure.Identity;

using CulinaryBlog.Application.Abstractions.Identity;
using CulinaryBlog.Domain.Users;
using CulinaryBlog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

public sealed class RefreshTokenRepository(ApplicationDbContext dbContext) : IRefreshTokenRepository, IAsyncDisposable
{
    private IDbContextTransaction? _transaction;

    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken = default)
    {
        await dbContext.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var candidate = await dbContext.RefreshTokens.AsNoTracking()
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
        if (candidate is null) return null;

        if (dbContext.Database.CurrentTransaction is null)
        {
            _transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        }

        // Serialize rotation/logout/reuse for the whole user, including different sessions.
        // Re-read after acquiring the transaction lock so a concurrent rotation sees revocation.
        var lockKey = $"auth-refresh:{candidate.UserId}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0))", cancellationToken);

        return await dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(string userId, DateTimeOffset revokedAt, CancellationToken cancellationToken = default)
    {
        var activeTokens = await dbContext.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.Revoke(revokedAt);
        }
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null) await _transaction.DisposeAsync();
    }
}
