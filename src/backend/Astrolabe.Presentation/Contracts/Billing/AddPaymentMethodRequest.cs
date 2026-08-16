using Astrolabe.Domain.Features.Billing.Enums;

namespace Astrolabe.Presentation.Contracts.Billing;

/// <summary>
/// The body of a card being put on file.
///
/// <para>
/// <b>There is deliberately no field for a card number, a CVV or an expiry year beyond two digits.</b>
/// These are the display details a payment provider returns after tokenising. A client sending a
/// full number has nowhere to put it, and if it arrives in <c>Last4</c> the domain refuses the
/// request rather than truncating it into storage.
/// </para>
/// </summary>
public sealed record AddPaymentMethodRequest(
    CardBrand Brand,
    string Last4,
    string ExpiryMonthYear,
    string CardholderName,
    bool MakePrimary);
