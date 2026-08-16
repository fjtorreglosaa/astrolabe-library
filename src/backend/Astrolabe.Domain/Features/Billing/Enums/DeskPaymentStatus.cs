namespace Astrolabe.Domain.Features.Billing.Enums;

/// <summary>
/// Where a desk payment code stands.
///
/// <c>Expired</c> is written when something acts on a stale code, and derived when one is read — the
/// same reasoning as overdue in <c>reservations</c>. A job that failed would otherwise leave expired
/// codes looking valid at the counter.
/// </summary>
public enum DeskPaymentStatus
{
    Pending = 0,
    Validated = 1,
    Rejected = 2,
    Expired = 3
}
