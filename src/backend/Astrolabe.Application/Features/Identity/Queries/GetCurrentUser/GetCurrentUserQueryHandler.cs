using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Membership;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IIdentityUnitOfWork identity,
    ICurrentUser currentUser,
    IEntitlementProvider entitlements) : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(
        GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<CurrentUserDto>(IdentityErrors.InvalidCredentials);
        }

        // Read from the database rather than from the token: the role in a claim is a snapshot from
        // sign-in, and a role revocation since then must be reflected. The plan never appears in a
        // token at all — it changes far more often than a session lives.
        var user = await identity.Users.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.Status is not UserStatus.Active)
        {
            return Result.Failure<CurrentUserDto>(IdentityErrors.InvalidCredentials);
        }

        // Asked only for a member. Staff hold no subscription, and MemberEntitlement.None reports
        // Basic for them — reporting that as a plan would put a tier on an account that never
        // bought one, which is the very confusion GLOBAL-019 set out to end.
        PlanTier? plan = user.Role.IsMember()
            ? (await entitlements.GetForMemberAsync(user.Id, cancellationToken)).Plan
            : null;

        return Result.Success(new CurrentUserDto(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.Role,
            plan,
            user.CountryId,
            user.CityId,
            user.Role.IsStaff()));
    }
}
