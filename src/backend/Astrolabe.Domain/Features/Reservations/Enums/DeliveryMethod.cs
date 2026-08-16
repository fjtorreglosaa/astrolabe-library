namespace Astrolabe.Domain.Features.Reservations.Enums;

/// <summary>How the copy reaches the member. Implements BR-RSV-003.</summary>
public enum DeliveryMethod
{
    /// <summary>"Ready in 2 h" at the library holding the copy. Free.</summary>
    Collection = 0,

    /// <summary>"24–48 h" to the member's address. Adds $3.99.</summary>
    HomeDelivery = 1
}
