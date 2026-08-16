namespace Astrolabe.Domain.Features.Store.Enums;

/// <summary>How a purchased book reaches the buyer. Implements BR-STR-010.</summary>
public enum OrderFulfilment
{
    /// <summary>"Ready in 2 h" at the library. Free.</summary>
    Collection = 0,

    /// <summary>"3–5 days" to the member's address. Adds $3.99 once per order.</summary>
    Shipping = 1
}
