namespace Astrolabe.Infrastructure.Features.Billing;

/// <summary>
/// How often the fine sweep runs, and how much it takes at a time. Supplied through
/// <c>IOptions&lt;T&gt;</c> per SDD+ section 9.1.
/// </summary>
public sealed class FineAccrualOptions
{
    public const string SectionName = "FineAccrual";

    /// <summary>Off in tests and any run that must be deterministic.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Daily. The event handler is what makes a fine appear immediately; this only catches what it
    /// missed, and sweeping more often would spend queries on a case that should be rare.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Bounded so one sweep cannot load every late return in the network. What is left over is
    /// picked up next time, because the query orders by check-in date.
    /// </summary>
    public int BatchSize { get; set; } = 500;
}
