namespace Astrolabe.Application.Contracts.Mail;

/// <summary>
/// Outcome of handing a message to the provider. Accepted means the provider took responsibility
/// for it, not that it reached the inbox — final delivery is asynchronous and outside our control.
/// </summary>
public sealed record EmailDeliveryResult
{
    private EmailDeliveryResult(bool accepted, string? providerMessageId, string? failureReason)
    {
        Accepted = accepted;
        ProviderMessageId = providerMessageId;
        FailureReason = failureReason;
    }

    public bool Accepted { get; }

    /// <summary>Provider-assigned identifier, used to correlate with delivery logs and webhooks.</summary>
    public string? ProviderMessageId { get; }

    /// <summary>Why the provider rejected the message. Safe to log; never surfaced to an end user.</summary>
    public string? FailureReason { get; }

    public static EmailDeliveryResult Success(string? providerMessageId) => new(true, providerMessageId, null);

    public static EmailDeliveryResult Failure(string reason) => new(false, null, reason);
}
