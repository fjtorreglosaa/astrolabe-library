namespace Astrolabe.Domain.Features.Membership.Enums;

/// <summary>
/// How far a plan lets a member borrow. Reach restricts borrowing only — every plan browses the
/// whole network (BR-MBR-006).
/// </summary>
public enum ReachKind
{
    /// <summary>Basic: the member's home library, and nothing else.</summary>
    HomeLibraryOnly = 0,

    /// <summary>Plus: every library in the member's city of residence.</summary>
    City = 1,

    /// <summary>Max: every library in the network.</summary>
    Network = 2
}
