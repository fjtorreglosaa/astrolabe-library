using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Application.Shared.Recommendations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Features.Recommendations.Entities;
using Astrolabe.Domain.Features.Recommendations.Enums;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Astrolabe.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Features.Recommendations;

/// <summary>
/// The fallback chain, in one place. Implements BR-REC-003 and BR-REC-007.
///
/// <para>
/// Model → previous set → most-borrowed, and the last one cannot fail. Every step that can fail
/// falls through to the next, so there is no path from here to an error on a member's screen. Both
/// callers — reading a stale cache and regenerating on purpose — go through this, because a member
/// who presses refresh must not be able to reach a different answer from the one the screen shows.
/// </para>
/// </summary>
public sealed class RecommendationGenerator(
    IRecommendationsUnitOfWork recommendations,
    IAiProviderRegistry providers,
    ISecretProtector protector,
    IReadingProfileBuilder profiles,
    IFallbackRecommender fallback,
    ILibraryLocationProvider libraries,
    AstrolabeDbContext context,
    IDateTimeProvider clock,
    ILogger<RecommendationGenerator> logger) : IRecommendationGenerator
{
    /// <summary>How many suggestions a set holds. The prototype shows four.</summary>
    private const int SuggestionCount = 4;

    /// <summary>
    /// How long a set stays fresh. A day: long enough that a member browsing twice does not pay for
    /// two generations, short enough that a week of reading changes the answer.
    /// </summary>
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(24);

    public async Task<RecommendationSetDto> GenerateAsync(
        Guid memberId,
        MemberEntitlement member,
        RecommendationSet? previous,
        CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var fromModel = await TryModelAsync(memberId, member, now, cancellationToken);

        if (fromModel is not null)
        {
            await ReplaceAsync(memberId, fromModel, cancellationToken);
            return await DescribeAsync(fromModel, cancellationToken);
        }

        // BR-REC-007. A stale personalised set beats a fresh generic one: it was right about this
        // member yesterday, and the fallback never was.
        if (previous is { Source: RecommendationSource.Model })
        {
            logger.LogWarning(
                "Serving a stale recommendation set for member {MemberId}; generation failed.",
                memberId);

            return await DescribeAsync(previous, cancellationToken);
        }

        var items = await BuildItemsAsync(
            await fallback.GetAsync(memberId, SuggestionCount, cancellationToken));

        var set = RecommendationSet.FromFallback(memberId, items, now, Lifetime);

        await ReplaceAsync(memberId, set, cancellationToken);

        return await DescribeAsync(set, cancellationToken);
    }

    /// <summary>
    /// Returns null on every failure — a plan without a connected library, an unreadable credential,
    /// a vendor that refused or timed out, an answer with nothing usable in it. The caller's job is
    /// to fall back, and giving it one thing to check keeps that honest.
    /// </summary>
    private async Task<RecommendationSet?> TryModelAsync(
        Guid memberId, MemberEntitlement member, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (member.CityId is not { } cityId)
        {
            return null;
        }

        var locations = await libraries.GetAllAsync(cancellationToken);

        var inCity = locations.Values
            .Where(location => location.CityId == cityId)
            .Select(location => location.LibraryId)
            .ToList();

        // BR-REC-003: any connected library in the member's city, not only their home one.
        var configuration = (await recommendations.Configurations
                .GetByLibrariesAsync(inCity, cancellationToken))
            .FirstOrDefault(candidate => candidate.IsConnected);

        if (configuration is null)
        {
            return null;
        }

        var credential = protector.Unprotect(configuration.Credential);

        if (credential is null)
        {
            // A key ring rotated past this ciphertext. The library is effectively unconfigured, and
            // its staff need to see that rather than have members silently fall back forever.
            logger.LogError(
                "Could not decrypt the credential for library {LibraryId}. Marking it unverified.",
                configuration.LibraryId);

            configuration.MarkFailed(now);
            await recommendations.SaveChangesAsync(cancellationToken);

            return null;
        }

        var profile = await profiles.BuildAsync(memberId, cancellationToken);

        // Nothing to personalise from. A model asked to invent a reading history produces confident
        // noise, so the most-borrowed list is the more honest answer.
        if (profile.IsEmpty)
        {
            return null;
        }

        var provider = providers.For(configuration.Provider);

        var suggestions = await provider.SuggestAsync(
            credential, profile, SuggestionCount, cancellationToken);

        if (suggestions.Count == 0)
        {
            configuration.MarkFailed(now);
            await recommendations.SaveChangesAsync(cancellationToken);

            return null;
        }

        // BR-REC-009. The model was only offered candidates that have copies, and is held to it
        // anyway — a vendor is not a trusted source of catalogue identifiers.
        var candidates = profile.CandidateBookIds.Select(book => book.BookId).ToHashSet();

        var items = await BuildItemsAsync(
            [.. suggestions.Where(suggestion => candidates.Contains(suggestion.BookId))]);

        if (items.Count == 0)
        {
            return null;
        }

        var set = RecommendationSet.FromModel(
            memberId, configuration.LibraryId, items, now, Lifetime);

        return set.IsSuccess ? set.Value : null;
    }

    /// <summary>
    /// BR-REC-010 is enforced here by dropping, not by throwing: one bad suggestion in four should
    /// cost the member that suggestion, not the whole set.
    /// </summary>
    private static Task<List<RecommendationItem>> BuildItemsAsync(
        IReadOnlyList<ProviderSuggestion> suggestions) =>
        Task.FromResult(suggestions
            .Select(suggestion => RecommendationItem.Create(
                suggestion.BookId, suggestion.Reason, suggestion.MatchPercent))
            .Where(item => item.IsSuccess)
            .Select(item => item.Value)
            .ToList());

    /// <summary>One live set per member. The previous one is replaced rather than accumulated.</summary>
    private async Task ReplaceAsync(
        Guid memberId, RecommendationSet set, CancellationToken cancellationToken)
    {
        var existing = await recommendations.Sets.GetLatestForMemberAsync(memberId, cancellationToken);

        if (existing is not null)
        {
            recommendations.Sets.Remove(existing);
        }

        await recommendations.Sets.AddAsync(set, cancellationToken);
        await recommendations.SaveChangesAsync(cancellationToken);
    }

    public async Task<RecommendationSetDto> DescribeAsync(
        RecommendationSet set, CancellationToken cancellationToken = default)
    {
        var bookIds = set.Items.Select(item => item.BookId).ToList();

        var books = await context.Books
            .AsNoTracking()
            .Where(book => bookIds.Contains(book.Id))
            .Select(book => new { book.Id, book.Title, book.Author, book.CoverUrl })
            .ToListAsync(cancellationToken);

        var byId = books.ToDictionary(book => book.Id);

        var items = set.Items
            // A book removed from the catalogue since the set was cached is dropped rather than
            // rendered as a blank row. BR-REC-009 in its second reading: still borrowable, now.
            .Where(item => byId.ContainsKey(item.BookId))
            .Select(item => new RecommendationDto(
                item.BookId,
                byId[item.BookId].Title,
                byId[item.BookId].Author,
                byId[item.BookId].CoverUrl,
                item.Reason,
                item.MatchPercent))
            .ToList();

        return new RecommendationSetDto(
            set.Source.ToString(),
            set.Source is RecommendationSource.Model
                ? RecommendationCopy.ModelNote
                : RecommendationCopy.FallbackNote,
            set.GeneratedAt,
            // A fallback can always be refreshed: it costs nobody anything, and a member whose
            // library has just connected should see the difference at once.
            set.Source is RecommendationSource.Fallback
                || clock.UtcNow - set.GeneratedAt >= TimeSpan.FromHours(1),
            items);
    }
}
