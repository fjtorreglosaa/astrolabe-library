using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Recommendations.Entities;

/// <summary>
/// One suggestion. Implements BR-REC-009 and BR-REC-010.
///
/// <para>
/// The reason is required and the constructor refuses without one. A model that returns a title and
/// no justification would otherwise render a blank line beside a book, which a member reads as a
/// fault — so the suggestion is dropped before it can be, rather than checked at the point of
/// display where somebody will eventually forget.
/// </para>
/// </summary>
public sealed class RecommendationItem : Entity
{
    /// <summary>Long enough for the prototype's sentences, short enough that a model cannot ramble.</summary>
    public const int MaxReasonLength = 280;

    private RecommendationItem()
    {
    }

    private RecommendationItem(Guid id, Guid bookId, string reason, int matchPercent) : base(id)
    {
        BookId = bookId;
        Reason = reason;
        MatchPercent = matchPercent;
    }

    public Guid BookId { get; private set; }

    /// <summary>Why this book, in one sentence. Never empty — see BR-REC-010.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>
    /// The prototype's "94% match".
    ///
    /// Display copy supplied alongside the reason, not a computed ranking: nothing in the product
    /// states how it would be derived, and inventing an algorithm would be inventing product. Clamped
    /// rather than validated, because a vendor returning 103 is a reason to show 100, not to throw
    /// away an otherwise good suggestion. Recorded in `recommendations.business.md` §8.
    /// </summary>
    public int MatchPercent { get; private set; }

    public static Result<RecommendationItem> Create(Guid bookId, string reason, int matchPercent)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            return Result.Failure<RecommendationItem>(RecommendationErrors.ReasonRequired);
        }

        var trimmed = reason.Trim();

        if (trimmed.Length > MaxReasonLength)
        {
            trimmed = trimmed[..MaxReasonLength];
        }

        return Result.Success(new RecommendationItem(
            Guid.NewGuid(), bookId, trimmed, Math.Clamp(matchPercent, 0, 100)));
    }
}
