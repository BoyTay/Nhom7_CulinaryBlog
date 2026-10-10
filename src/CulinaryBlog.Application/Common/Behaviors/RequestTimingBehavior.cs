using MediatR;
using Microsoft.Extensions.Logging;

namespace CulinaryBlog.Application.Common.Behaviors;

public sealed class RequestTimingBehavior<TRequest, TResponse>(
    TimeProvider timeProvider,
    ILogger<RequestTimingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly Action<ILogger, string, double, Exception?> LogCompleted =
        LoggerMessage.Define<string, double>(LogLevel.Information, new EventId(7001, "RequestCompleted"),
            "MediatR {RequestName} completed in {ElapsedMilliseconds:F1} ms");

    private static readonly Action<ILogger, string, double, Exception?> LogSlow =
        LoggerMessage.Define<string, double>(LogLevel.Warning, new EventId(7002, "SlowRequest"),
            "MediatR {RequestName} exceeded 500 ms: {ElapsedMilliseconds:F1} ms");

    private static readonly Action<ILogger, string, string, double, Exception?> LogFailed =
        LoggerMessage.Define<string, string, double>(LogLevel.Warning, new EventId(7003, "RequestFailed"),
            "MediatR {RequestName} failed with {ExceptionType} after {ElapsedMilliseconds:F1} ms");

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var started = timeProvider.GetTimestamp();
        try
        {
            var response = await next(cancellationToken).ConfigureAwait(false);
            var elapsed = timeProvider.GetElapsedTime(started).TotalMilliseconds;
            if (elapsed > 500)
            {
                LogSlow(logger, requestName, elapsed, null);
            }
            else
            {
                LogCompleted(logger, requestName, elapsed, null);
            }

            return response;
        }
        catch (Exception exception)
        {
            LogFailed(logger, requestName, exception.GetType().Name,
                timeProvider.GetElapsedTime(started).TotalMilliseconds, null);
            throw;
        }
    }
}
