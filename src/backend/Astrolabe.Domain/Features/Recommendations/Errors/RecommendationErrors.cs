using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Recommendations.Errors;

public static class RecommendationErrors
{
    // ---------- Credentials, BR-REC-004 and BR-REC-008 ----------

    public static readonly Error CredentialEmpty =
        Error.Validation("recommendations.credential_empty", "Enter a valid provider key.");

    public static readonly Error CredentialKeyVersionMissing =
        Error.Validation("recommendations.credential_key_version_missing",
            "A stored credential must record which key encrypted it.");

    /// <summary>
    /// BR-REC-008. The prototype's own flow: the key is tested before anything goes live, and a key
    /// the provider rejects leaves the library exactly as unconnected as it was.
    /// </summary>
    public static readonly Error CredentialRejectedByProvider =
        Error.Validation("recommendations.credential_rejected",
            "That key was refused by the provider. The library is still not connected.");

    public static readonly Error CannotEnableAnUnverifiedCredential =
        Error.Conflict("recommendations.cannot_enable_unverified",
            "Save and test a key before switching recommendations on.");

    // ---------- Configuration ----------

    public static readonly Error ConfigurationNotFound =
        Error.NotFound("recommendations.configuration_not_found",
            "That library has no AI configuration.");

    public static readonly Error LibraryOutOfScope =
        Error.Authorization("recommendations.library_out_of_scope",
            "You can only configure libraries you administer.");

    // ---------- The member surface ----------

    /// <summary>
    /// BR-REC-002. Not "not found": the surface exists and is simply not part of their plan, which
    /// is what the prototype tells them.
    /// </summary>
    public static readonly Error PlanDoesNotIncludeRecommendations =
        Error.Authorization("recommendations.plan_excluded",
            "Personalised picks are part of the Plus and Max plans.");

    /// <summary>
    /// BR-REC-010. A suggestion with a blank justification reads as a fault to the member, so it
    /// never reaches them — the set refuses to be built rather than rendering an empty line.
    /// </summary>
    public static readonly Error ReasonRequired =
        Error.Validation("recommendations.reason_required",
            "A recommendation must say why it was chosen.");

    public static readonly Error NothingToRecommend =
        Error.Validation("recommendations.nothing_to_recommend",
            "A recommendation set must contain at least one suggestion.");

    /// <summary>BR-REC-011. Refreshing must not let a member spend their library's credit.</summary>
    public static readonly Error RegeneratedTooRecently =
        Error.Conflict("recommendations.regenerated_too_recently",
            "These were refreshed a moment ago. Try again shortly.");
}
