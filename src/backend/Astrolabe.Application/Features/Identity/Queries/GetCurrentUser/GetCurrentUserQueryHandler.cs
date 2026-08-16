using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Queries.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IIdentityUnitOfWork identity,
    ICurrentUser currentUser) : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(
        GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<CurrentUserDto>(IdentityErrors.InvalidCredentials);
        }

        // Read from the database rather than from the token: the role in a claim is a snapshot from
        // sign-in, and a plan change or a role revocation since then must be reflected.
        var user = await identity.Users.GetByIdAsync(userId, cancellationToken);

        if (user is null || user.Status is not UserStatus.Active)
        {
            return Result.Failure<CurrentUserDto>(IdentityErrors.InvalidCredentials);
        }

        return Result.Success(new CurrentUserDto(
            user.Id,
            user.Email.Value,
            user.FullName,
            user.Role,
            user.CountryId,
            user.CityId,
            user.Role.IsStaff()));
    }
}
