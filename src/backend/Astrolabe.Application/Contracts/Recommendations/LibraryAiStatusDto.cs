namespace Astrolabe.Application.Contracts.Recommendations;

/// <summary>
/// One library's AI configuration, for its staff.
///
/// <para>
/// <b>There is no credential field here in any form</b> — not the ciphertext, not a masked version,
/// not a last-four, not a length. BR-REC-004 says never returned by any API response, and the surest
/// way to honour that is for the shape to have nowhere to put one.
/// </para>
/// </summary>
/// <param name="Status">The prototype's own words: "{provider} connected" or "Not configured".</param>
/// <param name="Note">What it means for that library's members.</param>
public sealed record LibraryAiStatusDto(
    Guid LibraryId,
    string LibraryName,
    string? Provider,
    bool IsConnected,
    bool IsEnabled,
    bool IsVerified,
    DateTimeOffset? LastVerifiedAt,
    string Status,
    string Note);
