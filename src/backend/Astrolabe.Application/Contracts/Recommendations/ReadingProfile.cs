namespace Astrolabe.Application.Contracts.Recommendations;

/// <summary>
/// Everything a provider is ever told about a member. Implements BR-REC-005.
///
/// <para>
/// <b>There is deliberately no identifier, no name, no email and no individual reservation here.</b>
/// The rule says only aggregated, anonymised reading data may be sent, and this record is how that
/// is enforced: a provider client cannot include a member's name because it is never handed one.
/// Adding a field to this type is the one change that could break BR-REC-005, which is exactly why
/// there is only one type and only one builder.
/// </para>
/// </summary>
/// <param name="Genres">Genres the member has borrowed, most frequent first. Names, not counts.</param>
/// <param name="RecentTitles">
/// Titles read recently, as bare strings. A title is not personal data — it is what the model needs
/// to say "similar in tone to X", which the prototype's own copy does.
/// </param>
/// <param name="CandidateBookIds">
/// What the model may choose from: books with at least one copy in the catalogue. BR-REC-009 is
/// enforced by never offering anything else rather than by filtering the answer afterwards.
/// </param>
public sealed record ReadingProfile(
    IReadOnlyList<string> Genres,
    IReadOnlyList<string> RecentTitles,
    IReadOnlyList<CandidateBook> CandidateBookIds)
{
    /// <summary>True when there is nothing to personalise from. The caller serves the fallback.</summary>
    public bool IsEmpty => Genres.Count == 0 && RecentTitles.Count == 0;
}
