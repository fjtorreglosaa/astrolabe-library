namespace Astrolabe.Domain.Features.Membership.Enums;

/// <summary>
/// Something a member gives up by moving down a plan, as BR-MBR-020 enumerates it.
///
/// <para>
/// Enumerated rather than phrased, for the same reason catalog rejections are: the wording is fixed
/// by the prototype and must be identical in every surface, and a string in the domain drifts the
/// moment two call sites format it.
/// </para>
/// <para>
/// Deliberately short. A downgrade also narrows reach from the network to a city and shrinks the
/// purchase discount, but the prototype's <c>losing</c> list names neither, and the prototype has
/// the final word on what the member is told. Adding disclosures it does not make would put this
/// list out of step with the screen it exists to fill.
/// </para>
/// </summary>
public enum PlanChangeLoss
{
    /// <summary>Reward points stop accruing and stop being redeemable. Leaving Max.</summary>
    RewardPoints,

    /// <summary>Borrowing narrows to the home library and the Basic catalogue. Moving to Basic.</summary>
    HomeLibraryAndBasicCatalog,

    /// <summary>AI recommendations turn off. Moving to Basic.</summary>
    Recommendations
}
