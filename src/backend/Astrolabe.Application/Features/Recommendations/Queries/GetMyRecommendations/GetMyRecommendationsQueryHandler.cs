using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Application.Shared.Recommendations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Features.Recommendations.Policies;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Recommendations.Queries.GetMyRecommendations;

/// <summary>
/// Where BR-REC-002, BR-REC-003, BR-REC-006 and BR-REC-007 meet.
///
/// <para>
/// The order matters and is the whole handler: plan first, then cache, then the model, then the
/// fallback. Every step that can fail falls through to the next, and the last one cannot fail — so
/// there is no path from here to an error on a member's screen, which is what BR-REC-007 asks for.
/// </para>
/// </summary>
public sealed class GetMyRecommendationsQueryHandler(
    IRecommendationsUnitOfWork recommendations,
    IRecommendationGenerator generator,
    IEntitlementProvider entitlements,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<GetMyRecommendationsQuery, RecommendationSetDto>
{
    public async Task<Result<RecommendationSetDto>> Handle(
        GetMyRecommendationsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<RecommendationSetDto>(
                RecommendationErrors.PlanDoesNotIncludeRecommendations);
        }

        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);

        // BR-REC-002, checked before anything is read or generated. A Basic member must not even
        // cause a cache lookup, let alone a provider call somebody else is paying for.
        if (!RecommendationAccessPolicy.PlanIncludesRecommendations(member.Plan))
        {
            return Result.Failure<RecommendationSetDto>(
                RecommendationErrors.PlanDoesNotIncludeRecommendations);
        }

        // BR-REC-006. A fresh set is served as it is, whatever it cost to make.
        var cached = await recommendations.Sets.GetLatestForMemberAsync(memberId, cancellationToken);

        if (cached is not null && cached.IsFresh(clock.UtcNow))
        {
            return Result.Success(await generator.DescribeAsync(cached, cancellationToken));
        }

        // Stale or missing. Generating may still fail, and BR-REC-007 says the stale one is better
        // than an error — so the previous set is handed to the generator as its own last resort.
        var set = await generator.GenerateAsync(memberId, member, cached, cancellationToken);

        return Result.Success(set);
    }
}
