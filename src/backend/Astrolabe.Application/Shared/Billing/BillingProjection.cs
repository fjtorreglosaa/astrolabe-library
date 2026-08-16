using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;

namespace Astrolabe.Application.Shared.Billing;

/// <summary>
/// Turns billing entities into the shapes the interface renders.
///
/// Expiry is computed here, server-side, and travels as a flag. A browser in another zone deciding a
/// code was still valid would send a member to a counter that has to refuse them.
/// </summary>
public static class BillingProjection
{
    public static FineDto ToDto(
        Fine fine,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries) =>
        new(fine.Id,
            fine.BookTitle,
            // The prototype's own phrasing: "20 days late".
            $"{fine.DaysLate} {(fine.DaysLate == 1 ? "day" : "days")} late",
            fine.DaysLate,
            (int)fine.Amount.Cents,
            fine.Status.ToString(),
            fine.AssessedAt,
            libraries.GetValueOrDefault(fine.LibraryId)?.LibraryName ?? "Unknown library");

    public static DeskPaymentDto ToDto(
        DeskPayment payment,
        IReadOnlyList<Fine> fines,
        string memberName,
        IReadOnlyDictionary<Guid, BookProjection.LibraryLocation> libraries,
        DateTimeOffset now)
    {
        var location = libraries.GetValueOrDefault(payment.LibraryId);

        return new DeskPaymentDto(
            payment.Id,
            payment.Code.Value,
            memberName,
            (int)payment.Amount.Cents,
            payment.Status.ToString(),
            payment.IsExpiredAt(now),
            location is null ? "Unknown library" : $"{location.CityName} — {location.LibraryName}",
            ConceptFor(fines),
            payment.IssuedAt,
            payment.ExpiresAt,
            payment.RejectionReason);
    }

    public static LedgerEntryDto ToDto(LedgerEntry entry) =>
        new(entry.Id, entry.Kind.ToString(), (int)entry.Amount.Cents,
            entry.Description, entry.OccurredAt);

    public static PaymentMethodDto ToDto(PaymentMethod method) =>
        new(method.Id, method.Brand.ToString(), method.Last4, method.ExpiryMonthYear,
            method.CardholderName, method.IsPrimary, method.DisplayName);

    /// <summary>
    /// What the desk sees on the queue, in the prototype's wording: "Late fines · 2 titles", or the
    /// title itself when there is only one.
    /// </summary>
    private static string ConceptFor(IReadOnlyList<Fine> fines) => fines.Count switch
    {
        0 => "Late fines",
        1 => $"Late fine — {fines[0].BookTitle}",
        _ => $"Late fines · {fines.Count} titles"
    };

    /// <summary>Only what the member can pay by card. A held fine is owed but not payable.</summary>
    public static bool IsPayableByCard(Fine fine) => fine.Status is FineStatus.Outstanding;
}
