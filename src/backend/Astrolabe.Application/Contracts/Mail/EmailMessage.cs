namespace Astrolabe.Application.Contracts.Mail;

/// <summary>
/// A transactional email, described in provider-neutral terms.
/// The sender address is configuration, not a per-message concern, so it is not carried here.
/// </summary>
public sealed record EmailMessage
{
    public required string ToAddress { get; init; }

    public string? ToDisplayName { get; init; }

    public required string Subject { get; init; }

    /// <summary>Plain-text body. Always required: some clients never render HTML.</summary>
    public required string TextBody { get; init; }

    /// <summary>Optional HTML body. When present, clients that support it prefer this.</summary>
    public string? HtmlBody { get; init; }

    /// <summary>Formats the recipient as providers expect it, for example <c>Ada Lovelace &lt;ada@example.com&gt;</c>.</summary>
    public string FormattedRecipient => string.IsNullOrWhiteSpace(ToDisplayName)
        ? ToAddress
        : $"{ToDisplayName} <{ToAddress}>";
}
