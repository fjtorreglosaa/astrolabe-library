using Astrolabe.Domain.Abstractions.Persistence;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Middleware;

/// <summary>
/// Catches unexpected technical faults, logs them with a correlation identifier, and returns a
/// standardized problem response. Internal details never reach the client.
/// Expected business failures use the Result pattern instead and never arrive here.
/// See GUIDELINES.md sections 19 and 26.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.TraceIdentifier;

        // A concurrency conflict is the client's to retry, not a fault of the server, so it is
        // neither a 500 nor logged at error level.
        var isConflict = exception is ConcurrencyConflictException;
        var status = isConflict
            ? StatusCodes.Status409Conflict
            : StatusCodes.Status500InternalServerError;

        logger.Log(
            isConflict ? LogLevel.Warning : LogLevel.Error,
            exception,
            "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}, Method: {Method}",
            correlationId,
            httpContext.Request.Path,
            httpContext.Request.Method);

        var problem = new ProblemDetails
        {
            Status = status,
            Title = isConflict
                ? "Someone else changed this at the same time. Please try again."
                : "An unexpected error occurred.",
            Detail = isConflict
                ? "Reload and retry the operation."
                : "The request could not be completed. Quote the correlation identifier when reporting this.",
            Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}"
        };

        problem.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
