using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Primitives;

/// <summary>
/// Covers the Result pattern contract described in GUIDELINES.md section 17.
/// </summary>
[TestFixture]
public sealed class ResultTests
{
    [Test]
    public void Success_HasNoErrors()
    {
        var result = Result.Success();

        result.IsSuccess.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.Errors.Should().BeEmpty();
        result.Error.Should().Be(Error.None);
    }

    [Test]
    public void Failure_CarriesTheError()
    {
        var error = Error.NotFound("catalog.book_not_found", "Book not found.");

        var result = Result.Failure(error);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        result.Errors.Should().ContainSingle();
    }

    [Test]
    public void Failure_WithMultipleErrors_PreservesAllOfThem()
    {
        Error[] errors =
        [
            Error.Validation("identity.email_required", "Email is required."),
            Error.Validation("identity.password_too_short", "Password must be at least 12 characters.")
        ];

        var result = Result.Failure(errors);

        result.Errors.Should().HaveCount(2);
        result.Error.Should().Be(errors[0]);
    }

    [Test]
    public void SuccessOfT_ExposesTheValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Test]
    public void ValueOfFailedResult_Throws()
    {
        // Reading the value of a failure is a programming error, not a business outcome.
        var result = Result.Failure<int>(Error.Conflict("store.duplicate_order", "Order already exists."));

        var act = () => result.Value;

        act.Should().Throw<InvalidOperationException>();
    }

    [Test]
    public void ImplicitConversion_FromValue_ProducesSuccess()
    {
        Result<string> result = "astrolabe";

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("astrolabe");
    }

    [Test]
    public void ErrorFactories_SetTheMatchingType()
    {
        Error.Validation("a", "a").Type.Should().Be(ErrorType.Validation);
        Error.NotFound("b", "b").Type.Should().Be(ErrorType.NotFound);
        Error.Conflict("c", "c").Type.Should().Be(ErrorType.Conflict);
        Error.Authentication("d", "d").Type.Should().Be(ErrorType.Authentication);
        Error.Authorization("e", "e").Type.Should().Be(ErrorType.Authorization);
        Error.Domain("f", "f").Type.Should().Be(ErrorType.Domain);
        Error.Infrastructure("g", "g").Type.Should().Be(ErrorType.Infrastructure);
    }

    [Test]
    public void Error_CarriesStructuredMetadata()
    {
        var error = Error.Validation(
            "catalog.tier_not_in_plan",
            "This title is not in your plan.",
            new Dictionary<string, object?> { ["requiredTier"] = "Max" });

        error.Metadata.Should().ContainKey("requiredTier").WhoseValue.Should().Be("Max");
    }
}
