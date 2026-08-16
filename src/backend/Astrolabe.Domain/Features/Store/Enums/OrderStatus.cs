namespace Astrolabe.Domain.Features.Store.Enums;

/// <summary>
/// Where an order stands.
///
/// One member, because an order is created and paid in a single act and cannot be cancelled or
/// refunded in this stage. A wider enumeration would advertise states nothing can reach.
/// </summary>
public enum OrderStatus
{
    Paid = 0
}
