namespace Astrolabe.Application.Contracts.Billing;

/// <summary>
/// What the member owes, split by what they can act on.
///
/// <c>AwaitingValidationCents</c> is money still owed — a desk code holds it but nobody has paid —
/// so it is reported separately rather than folded into the payable total, which would invite the
/// member to pay it twice.
/// </summary>
public sealed record FinesSummaryDto(
    int OutstandingCents,
    int AwaitingValidationCents,
    int TotalOwedCents,
    int BalanceCents,
    IReadOnlyList<FineDto> Fines,
    IReadOnlyList<DeskPaymentDto> OpenDeskPayments);
