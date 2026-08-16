namespace Astrolabe.Domain.Features.Billing.Enums;

/// <summary>Where a fine stands. Implements BR-BIL-017 and BR-BIL-021.</summary>
public enum FineStatus
{
    /// <summary>Owed, and payable by card or at a desk.</summary>
    Outstanding = 0,

    /// <summary>
    /// Promised to a desk payment code. Still owed — nobody has paid — but no longer payable by
    /// card, or the member would pay twice for one debt.
    /// </summary>
    AwaitingValidation = 1,

    Paid = 2
}
