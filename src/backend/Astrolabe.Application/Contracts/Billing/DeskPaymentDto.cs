namespace Astrolabe.Application.Contracts.Billing;

/// <summary>
/// A desk payment code, for the member who holds it and for the desk that will take the money.
///
/// <c>IsExpired</c> is computed server-side from the clock. A browser in another zone deciding a
/// code was still valid would send a member to a counter that must refuse them.
/// </summary>
public sealed record DeskPaymentDto(
    Guid Id,
    string Code,
    string MemberName,
    int AmountCents,
    string Status,
    bool IsExpired,
    string LibraryName,
    string Concept,
    DateTimeOffset IssuedAt,
    DateTimeOffset ExpiresAt,
    string? RejectionReason);
