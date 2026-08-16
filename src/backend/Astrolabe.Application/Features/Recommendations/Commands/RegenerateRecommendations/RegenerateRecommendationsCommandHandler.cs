using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Recommendations;
using Astrolabe.Application.Contracts.Recommendations;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Recommendations.Errors;
using Astrolabe.Domain.Features.Recommendations.Policies;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Recommendations.Commands.RegenerateRecommendations;

public sealed class RegenerateRecommendationsCommandHandler(
    IRecommendationsUnitOfWork recommendations,
    IRecommendationGenerator generator,
    IEntitlementProvider entitlements,
    ICurrentUser currentUser,
    IDateTimeProvider clock)
    : ICommandHandler<RegenerateRecommendationsCommand, RecommendationSetDto>
{
    /// <summary>
    /// BR-REC-011. Proposed at one hour and recorded as `GLOBAL-023`, because the rule requires a
    /// limit and names no figure. The constant is here rather than in configuration so changing it
    /// is a decision somebody has to write down.
    /// </summary>
    public static readonly TimeSpan MinimumInterval = TimeSpan.FromHours(1);

    public async Task<Result<RecommendationSetDto>> Handle(
        RegenerateRecommendationsCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<RecommendationSetDto>(
                RecommendationErrors.PlanDoesNotIncludeRecommendations);
        }

        var member = await entitlements.GetForCurrentMemberAsync(cancellationToken);

        if (!RecommendationAccessPolicy.PlanIncludesRecommendations(member.Plan))
        {
            return Result.Failure<RecommendationSetDto>(
                RecommendationErrors.PlanDoesNotIncludeRecommendations);
        }

        var previous = await recommendations.Sets.GetLatestForMemberAsync(memberId, cancellationToken);
        var now = clock.UtcNow;

        // BR-REC-011. Refused rather than quietly served from cache: the member pressed a button and
        // is owed an answer about what happened, and "nothing, wait a bit" is a better answer than
        // a spinner that returns the same list.
        //
        // A fallback set does not count. It cost nobody anything, and a member whose library just
        // connected should not be made to wait an hour to see the difference.
        if (previous is { Source: Domain.Features.Recommendations.Enums.RecommendationSource.Model }
            && now - previous.GeneratedAt < MinimumInterval)
        {
            return Result.Failure<RecommendationSetDto>(
                RecommendationErrors.RegeneratedTooRecently);
        }

        var set = await generator.GenerateAsync(memberId, member, previous, cancellationToken);

        return Result.Success(set);
    }
}
