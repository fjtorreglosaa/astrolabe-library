namespace Astrolabe.Application.Contracts.Billing;

/// <summary>
/// One movement on the account statement. The amount is signed, so the interface renders it without
/// having to know which kinds are debits.
/// </summary>
public sealed record LedgerEntryDto(
    Guid Id,
    string Kind,
    int AmountCents,
    string Description,
    DateTimeOffset OccurredAt);
