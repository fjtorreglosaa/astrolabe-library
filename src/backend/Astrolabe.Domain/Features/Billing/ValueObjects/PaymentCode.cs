using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.ValueObjects;

/// <summary>
/// The <c>MP-…</c> code a member shows at a library desk.
///
/// Like the reservation handover code, it authorises nothing on its own: a librarian standing in
/// front of the member takes the money, and validation is a staff action guarded by library scope.
/// The code identifies the payment; it does not prove anything about who is holding it.
/// </summary>
public sealed record PaymentCode
{
    private const string Prefix = "MP-";

    private PaymentCode(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Five digits, in the prototype's own range. Short enough to read aloud across a counter, which
    /// is the only thing it has to be.
    /// </summary>
    public static PaymentCode Generate(Guid seed)
    {
        var hash = 0;

        foreach (var character in seed.ToString())
        {
            hash = (hash * 31 + character) % 90000;
        }

        return new PaymentCode($"{Prefix}{10000 + hash}");
    }

    public static Result<PaymentCode> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Result.Failure<PaymentCode>(BillingErrors.DeskPaymentNotFound);
        }

        var normalised = raw.Trim().ToUpperInvariant();

        if (!normalised.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return Result.Failure<PaymentCode>(BillingErrors.DeskPaymentNotFound);
        }

        return Result.Success(new PaymentCode(normalised));
    }

    public override string ToString() => Value;
}
