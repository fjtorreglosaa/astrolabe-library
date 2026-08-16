namespace Astrolabe.Application.Features.Identity.Commands.RevokeSessions;

/// <summary>
/// Which sessions a revocation targets. Modelled as a scope rather than three near-identical
/// commands, so BR-IDN-025 — you may only revoke your own — is enforced in exactly one place.
/// </summary>
public enum RevocationScope
{
    /// <summary>Only the sessions named in the request.</summary>
    Specified = 0,

    /// <summary>Every session except the one making the request.</summary>
    AllOthers = 1,

    /// <summary>Every session, including the one making the request.</summary>
    All = 2
}
