namespace Astrolabe.Infrastructure.Features.Membership;

/// <summary>
/// How often the plan renewal sweep runs, and how much it takes at a time.
///
/// Configurable rather than hard-coded so a test or a local run can turn it off: a background sweep
/// that cannot be disabled makes every integration test race against it.
/// </summary>
public sealed class PlanRenewalOptions
{
    public const string SectionName = "PlanRenewal";

    /// <summary>Off in tests and in any run that must be deterministic.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// An hour. A renewal is a date, not a moment, so sweeping more often buys nothing — and the
    /// read path already applies a due change the instant the member appears.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Bounded so one sweep cannot load every subscription in the network into memory. Whatever is
    /// left over is picked up on the next tick, because the query orders by renewal date.
    /// </summary>
    public int BatchSize { get; set; } = 200;
}
