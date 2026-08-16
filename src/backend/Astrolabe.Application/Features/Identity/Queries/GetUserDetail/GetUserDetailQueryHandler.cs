using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Application.Shared.Identity;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Queries.GetUserDetail;

public sealed class GetUserDetailQueryHandler(
    IIdentityUnitOfWork identity,
    IEntitlementProvider entitlements,
    IMemberActivityProbe activity,
    ILibraryScopeProvider scope,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser) : IQueryHandler<GetUserDetailQuery, UserDetailDto>
{
    public async Task<Result<UserDetailDto>> Handle(
        GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        if (currentUser is not { UserId: { } actorId, Role: { } actorRole })
        {
            return Result.Failure<UserDetailDto>(NetworkErrors.StaffRequired);
        }

        if (!actorRole.IsStaff())
        {
            return Result.Failure<UserDetailDto>(NetworkErrors.StaffRequired);
        }

        var user = await identity.Users.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return Result.Failure<UserDetailDto>(IdentityErrors.AccountNotFound);
        }

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);

        // The same scope the listing applies. Without it an administrator could open any account by
        // guessing an identifier, and the listing's filter would be decoration.
        if (!reach.IsUnrestricted)
        {
            var covered = user.CityId is { } cityId
                && locations.Values.Any(l => l.CityId == cityId && reach.Covers(l.LibraryId));

            if (!covered)
            {
                return Result.Failure<UserDetailDto>(IdentityErrors.AccountOutOfScope);
            }
        }

        // Asked only for a member. Staff hold no subscription, and MemberEntitlement.None reports
        // Basic for them — showing that would put a tier on an account that never bought one.
        PlanTier? plan = user.Role.IsMember()
            ? (await entitlements.GetForMemberAsync(user.Id, cancellationToken)).Plan
            : null;

        var stats = user.Role.IsMember()
            ? await activity.GetAsync(user.Id, cancellationToken)
            : MemberActivity.None;

        var homeLibraries = await libraries.GetHomeLibraryByCityAsync(cancellationToken);

        return Result.Success(UserProjection.ToDetail(
            user, actorId, actorRole, plan, locations, homeLibraries,
            stats.LastActiveAt, stats.ActiveReservations, stats.OutstandingFineCents,
            stats.Purchases, stats.OnTimeReturnPercent));
    }
}
