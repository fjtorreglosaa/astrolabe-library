namespace Astrolabe.Application.Features.Identity.Commands.AdministerUser;

/// <summary>
/// What a staff user is doing to an account from the directory. Transcribed from the prototype's
/// four confirmations: block, unblock, remove, restore.
///
/// <para>
/// One command with an action rather than four commands, and deliberately: every one of them shares
/// the same authority check, the same scope check and the same audit write, and the part that
/// differs is a single call on the aggregate. Four handlers would be four places for the guard to
/// drift, which is exactly the failure BR-NET-008 cannot afford.
/// </para>
/// </summary>
public enum UserAdministrationAction
{
    /// <summary>BR-IDN-007. Ends every live session at the moment of blocking.</summary>
    Block = 0,

    /// <summary>Returns a blocked account to service.</summary>
    Unblock = 1,

    /// <summary>BR-IDN-008. Hides the account and keeps its history.</summary>
    Delete = 2,

    /// <summary>Returns a deleted account to the directory.</summary>
    Restore = 3
}
