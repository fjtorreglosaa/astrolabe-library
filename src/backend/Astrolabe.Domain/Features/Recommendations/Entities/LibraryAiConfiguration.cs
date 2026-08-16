using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Recommendations.Enums;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Features.Recommendations.Events;
using Astrolabe.Domain.Features.Recommendations.ValueObjects;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Recommendations.Entities;

/// <summary>
/// What one library has connected. Implements BR-REC-001, BR-REC-008 and BR-REC-012.
///
/// <para>
/// One row per library, because that is the whole point of the domain: two branches of the same
/// network can answer differently, and one can be paying a vendor while the other is not.
/// </para>
/// </summary>
public sealed class LibraryAiConfiguration : AggregateRoot
{
    private LibraryAiConfiguration()
    {
    }

    private LibraryAiConfiguration(
        Guid id, Guid libraryId, AiProvider provider, EncryptedSecret credential,
        DateTimeOffset now) : base(id)
    {
        LibraryId = libraryId;
        Provider = provider;
        Credential = credential;
        CreatedAt = now;
    }

    public Guid LibraryId { get; private set; }

    public AiProvider Provider { get; private set; }

    /// <summary>Ciphertext. BR-REC-004 — there is no path from here to the plaintext.</summary>
    public EncryptedSecret Credential { get; private set; } = null!;

    /// <summary>BR-REC-008. False until the credential has answered its provider.</summary>
    public bool IsVerified { get; private set; }

    /// <summary>
    /// BR-REC-012. Deliberately independent of <see cref="IsVerified"/>: switching off must preserve
    /// the credential, and one flag would force a re-verification on every re-enable — spending the
    /// library's money to learn what was already known.
    /// </summary>
    public bool IsEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? LastVerifiedAt { get; private set; }

    public DateTimeOffset? LastFailureAt { get; private set; }

    /// <summary>
    /// The single question every other part of the system asks. BR-REC-003 turns on exactly this.
    /// </summary>
    public bool IsConnected => IsEnabled && IsVerified;

    public static Result<LibraryAiConfiguration> Configure(
        Guid libraryId, AiProvider provider, EncryptedSecret credential, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(credential);

        // Created unverified and switched off. The prototype's button is "Save and test", and until
        // the test passes the library is exactly as unconnected as it was before.
        return Result.Success(new LibraryAiConfiguration(
            Guid.NewGuid(), libraryId, provider, credential, now));
    }

    /// <summary>The provider accepted the credential. BR-REC-008.</summary>
    public void MarkVerified(DateTimeOffset now)
    {
        IsVerified = true;
        LastVerifiedAt = now;
        LastFailureAt = null;
    }

    /// <summary>
    /// The provider refused, or stopped answering. BR-REC-007 and BR-REC-008.
    ///
    /// Drops <see cref="IsVerified"/> rather than <see cref="IsEnabled"/>, so the library's own
    /// decision to offer recommendations survives a vendor outage and its staff see "Not configured"
    /// — which tells them to fix a key rather than to flip a switch they never touched.
    /// </summary>
    public void MarkFailed(DateTimeOffset now)
    {
        IsVerified = false;
        LastFailureAt = now;
    }

    public Result Enable()
    {
        if (!IsVerified)
        {
            return Result.Failure(RecommendationErrors.CannotEnableAnUnverifiedCredential);
        }

        IsEnabled = true;
        return Result.Success();
    }

    /// <summary>
    /// BR-REC-012. Immediate for members, and the credential stays exactly where it was.
    ///
    /// The event is what makes "immediate" true without every caller remembering: cached sets for
    /// this library's city are evicted by a handler, so a member holding a fresh set falls back on
    /// their next read rather than at expiry.
    /// </summary>
    public void Disable(DateTimeOffset now)
    {
        if (!IsEnabled)
        {
            return;
        }

        IsEnabled = false;

        Raise(new LibraryAiDisabled(Guid.NewGuid(), now, LibraryId));
    }

    /// <summary>
    /// A new key, or a different provider. Returns to unverified, because a credential nobody has
    /// tested is not one BR-REC-008 lets go live — including a replacement for one that worked.
    /// </summary>
    public Result Replace(AiProvider provider, EncryptedSecret credential, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(credential);

        Provider = provider;
        Credential = credential;
        IsVerified = false;
        LastFailureAt = null;
        LastVerifiedAt = null;

        // Enabled is untouched: a library that had switched recommendations on has not changed its
        // mind by rotating a key, and re-verifying will bring it straight back.
        return Result.Success();
    }
}
