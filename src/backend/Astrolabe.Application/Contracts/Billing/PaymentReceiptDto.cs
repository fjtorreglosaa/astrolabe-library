namespace Astrolabe.Application.Contracts.Billing;

/// <summary>What the member is shown after paying by card.</summary>
public sealed record PaymentReceiptDto(
    string Receipt,
    int AmountCents,
    string PaidWith,
    int FineCount,
    DateTimeOffset PaidAt);
