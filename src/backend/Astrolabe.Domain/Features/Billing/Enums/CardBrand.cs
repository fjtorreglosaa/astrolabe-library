namespace Astrolabe.Domain.Features.Billing.Enums;

/// <summary>
/// The brand shown beside the last four digits. Display only — this system never sees enough of a
/// card to determine the brand itself, so the caller reports what their provider said.
/// </summary>
public enum CardBrand
{
    Visa = 0,
    Mastercard = 1,
    Amex = 2,
    Other = 3
}
