namespace Astrolabe.Domain.Primitives;

/// <summary>
/// A structured, strongly typed application error. Error definitions are reusable and must never be
/// expressed as magic strings at the call site. See GUIDELINES.md section 18.
/// </summary>
public sealed record Error
{
    /// <summary>Represents the absence of an error. Only ever carried by a successful result.</summary>
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Domain);

    private Error(string code, string message, ErrorType type, IReadOnlyDictionary<string, object?>? metadata = null)
    {
        Code = code;
        Message = message;
        Type = type;
        Metadata = metadata ?? new Dictionary<string, object?>();
    }

    /// <summary>Stable machine-readable identifier, for example <c>catalog.copy_out_of_stock</c>.</summary>
    public string Code { get; }

    /// <summary>Human-readable description. Safe to surface to an end user.</summary>
    public string Message { get; }

    public ErrorType Type { get; }

    /// <summary>Additional structured context, for example the offending field or identifier.</summary>
    public IReadOnlyDictionary<string, object?> Metadata { get; }

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorType.Validation, metadata);

    public static Error NotFound(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorType.NotFound, metadata);

    public static Error Conflict(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorType.Conflict, metadata);

    public static Error Authentication(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorType.Authentication, metadata);

    public static Error Authorization(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorType.Authorization, metadata);

    public static Error Domain(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorType.Domain, metadata);

    public static Error Infrastructure(string code, string message, IReadOnlyDictionary<string, object?>? metadata = null)
        => new(code, message, ErrorType.Infrastructure, metadata);

    public override string ToString() => $"{Type}:{Code}";
}
