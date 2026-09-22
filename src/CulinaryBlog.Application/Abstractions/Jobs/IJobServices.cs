namespace CulinaryBlog.Application.Abstractions.Jobs;

public interface IJobExecutionStore
{
    Task<bool> TryClaimAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task MarkCompletedAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}

public interface IWelcomeEmailSender
{
    Task SendAsync(string email, string displayName, CancellationToken cancellationToken = default);
}

public interface IRecipeImageProcessor
{
    Task ProcessAsync(Guid recipeImageId, string originalUrl, CancellationToken cancellationToken = default);
}

public interface ISitemapWriter
{
    Task GenerateAsync(CancellationToken cancellationToken = default);
}
