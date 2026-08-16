namespace Astrolabe.Application.Contracts.Billing;

/// <summary>One fine as the member reads it on the fines screen.</summary>
public sealed record FineDto(
    Guid Id,
    string BookTitle,
    string Reason,
    int DaysLate,
    int AmountCents,
    string Status,
    DateTimeOffset AssessedAt,
    string LibraryName);
