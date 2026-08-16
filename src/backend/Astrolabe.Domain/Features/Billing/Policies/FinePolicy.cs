using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Policies;

/// <summary>
/// What a late return costs. Implements BR-BIL-001 and BR-BIL-002.
///
/// <para>
/// A <b>pure static function</b> over a day count: no repository, no clock, no member. These are the
/// two numbers in the product that a mistake would mis-price for every member at once, so they live
/// in one place that can be exhaustively tested in milliseconds.
/// </para>
/// <para>
/// The prototype's seed confirms the arithmetic: "20 days late" is 700 cents and "11 days late" is
/// 385. Both are exactly days × 35.
/// </para>
/// </summary>
public static class FinePolicy
{
    /// <summary>BR-BIL-001. The prototype tells members: "a late return costs $0.35 a day per title".</summary>
    public static readonly Money PerDay = Money.FromCents(35);

    /// <summary>BR-BIL-002. A cap, not a rate change — 200 days late still costs this.</summary>
    public static readonly Money Cap = Money.FromUnits(9);

    /// <summary>
    /// The first whole day on which the cap binds. 25 days is $8.75 and 26 would be $9.10, so 26 is
    /// the answer. Exposed rather than left implicit so a test can assert the boundary rather than
    /// a magic number.
    /// </summary>
    public static int DaysToReachCap { get; } = (int)Math.Ceiling((double)Cap.Cents / PerDay.Cents);

    /// <summary>
    /// What <paramref name="daysLate"/> days cost.
    ///
    /// Zero or fewer costs nothing: BR-BIL-009 makes an on-time return produce no fine at all, and a
    /// negative day count — which `reservations` already floors — must never become a credit here.
    /// </summary>
    public static Money For(int daysLate)
    {
        if (daysLate <= 0)
        {
            return Money.Zero;
        }

        var raw = PerDay * daysLate;

        return raw.Cents >= Cap.Cents ? Cap : raw;
    }
}
