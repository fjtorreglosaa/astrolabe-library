using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Notifications.Commands.ClearNotifications;

public sealed class ClearNotificationsCommandHandler(
    INotificationsUnitOfWork notifications,
    ICurrentUser currentUser) : ICommandHandler<ClearNotificationsCommand>
{
    public async Task<Result> Handle(
        ClearNotificationsCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        // Scoped to the caller in the repository itself, so there is no query here that could be
        // written without the filter.
        await notifications.Notifications.ClearForMemberAsync(memberId, cancellationToken);
        await notifications.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
