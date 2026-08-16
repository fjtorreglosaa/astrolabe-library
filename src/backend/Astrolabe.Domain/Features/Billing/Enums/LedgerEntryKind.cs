namespace Astrolabe.Domain.Features.Billing.Enums;

/// <summary>What kind of movement an entry records.</summary>
public enum LedgerEntryKind
{
    /// <summary>Money owed: a fine, a delivery fee, a purchase. Held as a negative amount.</summary>
    Charge = 0,

    /// <summary>Money settled, by card or at a desk. Positive.</summary>
    Payment = 1,

    /// <summary>Money returned to the member, or a correction. Positive.</summary>
    Credit = 2
}
