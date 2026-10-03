using CulinaryBlog.Application.Common.Exceptions;
using CulinaryBlog.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CulinaryBlog.API.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private static readonly Action<ILogger, string, Exception?> LogClientError =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(4000, "ClientError"),
            "Request failed with {ErrorCode}");

    private static readonly Action<ILogger, string, Exception?> LogServerError =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(5000, "ServerError"),
            "Request failed with {ErrorCode}");

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title, type, errors) = exception switch
        {
            ApplicationValidationException validationException => (
                StatusCodes.Status422UnprocessableEntity,
                "Validation failed",
                "VALIDATION_ERROR",
                validationException.Errors),
            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictException.Code,
                null),
            UnauthorizedException unauthorizedException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                unauthorizedException.Code,
                null),
            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundException.Code,
                null),
            DomainException domainException => (
                StatusCodes.Status422UnprocessableEntity,
                "Business rule violation",
                domainException.Code,
                null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred",
                "INTERNAL_SERVER_ERROR",
                null),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            LogServerError(logger, type, exception);
        }
        else
        {
            LogClientError(logger, type, exception);
        }

        httpContext.Response.StatusCode = status;

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = type,
            Instance = httpContext.Request.Path,
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
