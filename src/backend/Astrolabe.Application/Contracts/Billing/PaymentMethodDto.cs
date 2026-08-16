namespace Astrolabe.Application.Contracts.Billing;

/// <summary>
/// A stored card, as far as this system knows it.
///
/// There is no card number here because there is none anywhere: the domain refuses anything but four
/// digits, and the column is four characters wide.
/// </summary>
public sealed record PaymentMethodDto(
    Guid Id,
    string Brand,
    string Last4,
    string ExpiryMonthYear,
    string CardholderName,
    bool IsPrimary,
    string DisplayName);
