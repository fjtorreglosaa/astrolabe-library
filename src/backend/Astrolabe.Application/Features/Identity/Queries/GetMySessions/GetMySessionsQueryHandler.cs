using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Identity.Queries.GetMySessions;

public sealed class GetMySessionsQueryHandler(IIdentityUnitOfWork identity,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<GetMySessionsQuery, IReadOnlyList<SessionDto>>
{
    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(
        GetMySessionsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } userId)
        {
            return Result.Failure<IReadOnlyList<SessionDto>>(IdentityErrors.InvalidCredentials);
        }

        // Scoped to the caller by construction, so BR-IDN-025 cannot be bypassed by any parameter.
        var live = await identity.Sessions.GetActiveByUserAsync(userId, clock.UtcNow, cancellationToken);

        IReadOnlyList<SessionDto> result =
        [
            .. live.Select(s => new SessionDto(
                s.Id,
                s.Device.Name,
                s.Device.Type,
                s.IpAddress,
                s.ApproximateLocation,
                s.CreatedAt,
                s.LastSeenAt,
                s.ExpiresAt,
                // BR-IDN-026: lets the interface mark "this device".
                s.Id == currentUser.SessionId))
        ];

        return Result.Success(result);
    }
}
