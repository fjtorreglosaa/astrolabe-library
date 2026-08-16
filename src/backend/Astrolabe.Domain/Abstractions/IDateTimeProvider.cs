namespace Astrolabe.Domain.Abstractions;

/// <summary>
/// Supplies the current instant. Injected rather than read from <see cref="DateTime"/> directly so
/// that time-dependent rules — loan due dates, fine accrual, token expiry — stay testable.
/// Always UTC, per BR-GLOBAL-002.
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
