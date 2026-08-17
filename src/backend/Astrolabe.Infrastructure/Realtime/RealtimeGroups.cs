namespace Astrolabe.Infrastructure.Realtime;

/// <summary>
/// The names of the groups a connection may join.
/// </summary>
/// <remarks>
/// One place, because a group name is an authorization boundary written as a string. The sender and
/// the joiner must agree exactly: a mismatch is not an error anywhere, it is simply a message that
/// reaches nobody — or, in the other direction, a member subscribed to a group they should not be
/// in. Both are silent, so neither name is written twice.
/// </remarks>
public static class RealtimeGroups
{
    /// <summary>Every device one member has open.</summary>
    public static string ForMember(Guid memberId) => $"member:{memberId:D}";

    /// <summary>Every signed-in administrator, of either rank.</summary>
    public const string Staff = "staff";
}
