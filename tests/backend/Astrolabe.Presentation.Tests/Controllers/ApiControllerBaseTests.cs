using Astrolabe.Domain.Primitives;
using Astrolabe.Presentation.Controllers;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace Astrolabe.Presentation.Tests.Controllers;

/// <summary>
/// Verifies the single responsibility of the base controller: turning a failed
/// <see cref="Result"/> into the correct HTTP status and an RFC 7807 payload.
/// </summary>
[TestFixture]
public sealed class ApiControllerBaseTests
{
    /// <summary>Minimal concrete controller exposing the protected conversion for testing.</summary>
    private sealed class TestController(ISender sender) : ApiControllerBase(sender)
    {
        public IActionResult Convert(Result result) => HandleFailure(result);
    }

    private static TestController CreateController()
    {
        var controller = new TestController(Mock.Of<ISender>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-1" }
            }
        };

        controller.HttpContext.Request.Method = "POST";
        controller.HttpContext.Request.Path = "/api/v1/reservations";

        return controller;
    }

    [TestCase(ErrorType.Validation, StatusCodes.Status400BadRequest)]
    [TestCase(ErrorType.NotFound, StatusCodes.Status404NotFound)]
    [TestCase(ErrorType.Conflict, StatusCodes.Status409Conflict)]
    [TestCase(ErrorType.Authentication, StatusCodes.Status401Unauthorized)]
    [TestCase(ErrorType.Authorization, StatusCodes.Status403Forbidden)]
    [TestCase(ErrorType.Domain, StatusCodes.Status422UnprocessableEntity)]
    [TestCase(ErrorType.Infrastructure, StatusCodes.Status500InternalServerError)]
    public void HandleFailure_MapsEachErrorTypeToItsStatusCode(ErrorType type, int expectedStatus)
    {
        var error = BuildError(type);
        var controller = CreateController();

        var response = controller.Convert(Result.Failure(error)) as ObjectResult;

        response.Should().NotBeNull();
        response!.StatusCode.Should().Be(expectedStatus);
    }

    [Test]
    public void HandleFailure_IncludesTheErrorCodeAndCorrelationId()
    {
        var controller = CreateController();
        var error = Error.NotFound("catalog.book_not_found", "Book not found.");

        var response = controller.Convert(Result.Failure(error)) as ObjectResult;
        var problem = response!.Value as ProblemDetails;

        problem.Should().NotBeNull();
        problem!.Title.Should().Be("Book not found.");
        problem.Extensions["code"].Should().Be("catalog.book_not_found");
        problem.Extensions["correlationId"].Should().Be("trace-1");
    }

    [Test]
    public void HandleFailure_WithASingleError_DoesNotEmitAnErrorsCollection()
    {
        var controller = CreateController();

        var response = controller.Convert(
            Result.Failure(Error.Validation("a", "a"))) as ObjectResult;
        var problem = response!.Value as ProblemDetails;

        problem!.Extensions.Should().NotContainKey("errors");
    }

    [Test]
    public void HandleFailure_WithSeveralErrors_EmitsThemAll()
    {
        var controller = CreateController();
        Error[] errors =
        [
            Error.Validation("identity.email_required", "Email is required."),
            Error.Validation("identity.password_too_short", "Password is too short.")
        ];

        var response = controller.Convert(Result.Failure(errors)) as ObjectResult;
        var problem = response!.Value as ProblemDetails;

        problem!.Extensions.Should().ContainKey("errors");
    }

    [Test]
    public void HandleFailure_OnASuccessfulResult_Throws()
    {
        var controller = CreateController();

        var act = () => controller.Convert(Result.Success());

        act.Should().Throw<InvalidOperationException>();
    }

    private static Error BuildError(ErrorType type) => type switch
    {
        ErrorType.Validation => Error.Validation("code", "message"),
        ErrorType.NotFound => Error.NotFound("code", "message"),
        ErrorType.Conflict => Error.Conflict("code", "message"),
        ErrorType.Authentication => Error.Authentication("code", "message"),
        ErrorType.Authorization => Error.Authorization("code", "message"),
        ErrorType.Domain => Error.Domain("code", "message"),
        _ => Error.Infrastructure("code", "message")
    };
}
