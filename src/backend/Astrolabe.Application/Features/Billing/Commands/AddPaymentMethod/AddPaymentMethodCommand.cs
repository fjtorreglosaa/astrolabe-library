using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Billing.Enums;

namespace Astrolabe.Application.Features.Billing.Commands.AddPaymentMethod;

/// <summary>
/// Puts a card on file. Implements BR-BIL-006.
///
/// <para>
/// <paramref name="Last4"/> is the <b>last four digits</b>, and the domain refuses anything else —
/// a full number is rejected rather than truncated. These are the details a payment provider returns
/// after tokenising, and they are the only shape this system is willing to accept.
/// </para>
/// </summary>
public sealed record AddPaymentMethodCommand(
    CardBrand Brand,
    string Last4,
    string ExpiryMonthYear,
    string CardholderName,
    bool MakePrimary) : ICommand<Guid>;
