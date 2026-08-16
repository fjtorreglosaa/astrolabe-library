namespace Astrolabe.Application.Contracts.Recommendations;

/// <summary>
/// A book the model is allowed to suggest. BR-REC-009: it has copies, so it can be borrowed.
///
/// The identifier travels so the answer can be matched back without trusting the model to spell a
/// title exactly as the catalogue does.
/// </summary>
public sealed record CandidateBook(Guid BookId, string Title, string Author, string Genre);
