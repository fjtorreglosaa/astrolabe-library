using System.Text.RegularExpressions;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Entities;

/// <summary>
/// A card a member has on file, as far as this system is ever allowed to know it.
/// Implements BR-BIL-006.
///
/// <para>
/// <b>There is no field here that could hold a card number.</b> These are the display details a
/// payment provider returns after tokenising — brand, last four, expiry, cardholder — and the
/// factory refuses anything longer than four digits rather than truncating it. That distinction
/// matters: truncating would mean a full number crossed the wire, reached this process and sat in
/// memory before being trimmed. Refusing means the caller is told to stop sending it.
/// </para>
/// <para>
/// The system is therefore <em>incapable</em> of storing a card number, not merely disinclined to.
/// </para>
/// </summary>
public sealed partial class PaymentMethod : Entity
{
    private PaymentMethod()
    {
    }

    private PaymentMethod(
        Guid id, Guid memberId, CardBrand brand, string last4,
        string expiry, string cardholderName, bool isPrimary) : base(id)
    {
        MemberId = memberId;
        Brand = brand;
        Last4 = last4;
        ExpiryMonthYear = expiry;
        CardholderName = cardholderName;
        IsPrimary = isPrimary;
    }

    public Guid MemberId { get; private set; }

    public CardBrand Brand { get; private set; }

    /// <summary>Exactly four digits. Never a prefix of anything longer.</summary>
    public string Last4 { get; private set; } = string.Empty;

    /// <summary>"09/28". Two digits, a slash, two digits.</summary>
    public string ExpiryMonthYear { get; private set; } = string.Empty;

    public string CardholderName { get; private set; } = string.Empty;

    /// <summary>The card the payment modal offers first.</summary>
    public bool IsPrimary { get; private set; }

    /// <summary>How the interface names it: "Visa •••• 4242".</summary>
    public string DisplayName => $"{Brand} •••• {Last4}";

    public static Result<PaymentMethod> Create(
        Guid memberId, CardBrand brand, string? last4,
        string? expiry, string? cardholderName, bool isPrimary)
    {
        // Exactly four, anchored at both ends. A thirteen-to-nineteen digit string fails here rather
        // than being silently reduced to its tail, so a caller who sent a real card number learns
        // that they must not.
        if (last4 is null || !FourDigits().IsMatch(last4))
        {
            return Result.Failure<PaymentMethod>(BillingErrors.CardDetailsInvalid);
        }

        if (expiry is null || !MonthYear().IsMatch(expiry.Trim()))
        {
            return Result.Failure<PaymentMethod>(BillingErrors.ExpiryInvalid);
        }

        if (string.IsNullOrWhiteSpace(cardholderName))
        {
            return Result.Failure<PaymentMethod>(BillingErrors.CardholderRequired);
        }

        return Result.Success(new PaymentMethod(
            Guid.NewGuid(), memberId, brand, last4,
            expiry.Trim(), cardholderName.Trim(), isPrimary));
    }

    public void MakePrimary() => IsPrimary = true;

    public void MakeSecondary() => IsPrimary = false;

    [GeneratedRegex(@"^\d{4}$")]
    private static partial Regex FourDigits();

    /// <summary>A month of 01 to 12 and a two-digit year. "13/28" is a typo, not an expiry.</summary>
    [GeneratedRegex(@"^(0[1-9]|1[0-2])/\d{2}$")]
    private static partial Regex MonthYear();
}
