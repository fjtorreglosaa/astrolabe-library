using Astrolabe.Domain.Primitives;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Astrolabe.Presentation.Controllers;

/// <summary>
/// Shared controller behaviour: dispatching through <see cref="ISender"/> and converting a
/// <see cref="Result"/> into an HTTP response. Nothing else belongs here — this base must not become
/// a miscellaneous utility class. See GUIDELINES.md sections 22 and 23.
/// </summary>
[ApiController]
public abstract class ApiControllerBase(ISender sender) : ControllerBase
{
    protected ISender Sender { get; } = sender;

    protected IActionResult HandleFailure(Result result)
    {
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("A successful result cannot be converted to a failure response.");
        }

        var error = result.Error;

        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Authentication => StatusCodes.Status401Unauthorized,
            ErrorType.Authorization => StatusCodes.Status403Forbidden,
            ErrorType.Domain => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = error.Message,
            Instance = $"{HttpContext.Request.Method} {HttpContext.Request.Path}"
        };

        problem.Extensions["code"] = error.Code;
        problem.Extensions["correlationId"] = HttpContext.TraceIdentifier;

        if (result.Errors.Count > 1)
        {
            problem.Extensions["errors"] = result.Errors
                .Select(e => new { code = e.Code, message = e.Message, type = e.Type.ToString() })
                .ToArray();
        }

        if (error.Metadata.Count > 0)
        {
            problem.Extensions["metadata"] = error.Metadata;
        }

        return StatusCode(status, problem);
    }
}
