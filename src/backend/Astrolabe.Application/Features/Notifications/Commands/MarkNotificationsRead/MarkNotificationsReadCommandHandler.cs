using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Notifications.Errors;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Notifications.Commands.MarkNotificationsRead;

public sealed class MarkNotificationsReadCommandHandler(
    INotificationsUnitOfWork notifications,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<MarkNotificationsReadCommand>
{
    public async Task<Result> Handle(
        MarkNotificationsReadCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var now = clock.UtcNow;

        if (request.NotificationId is not { } id)
        {
            await notifications.Notifications.MarkAllReadAsync(memberId, now, cancellationToken);
            await notifications.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }

        var notification = await notifications.Notifications.GetByIdAsync(id, cancellationToken);

        if (notification is null)
        {
            return Result.Failure(NotificationErrors.NotFound);
        }

        // BR-NTF-007. Checked against the token's member, never against anything in the request —
        // there is no field in the command that could name somebody else, and this is the guard that
        // makes that true rather than merely convenient.
        if (notification.MemberId != memberId)
        {
            return Result.Failure(NotificationErrors.NotYours);
        }

        // BR-NTF-006. Idempotent in the aggregate, so a double click commits the same state twice
        // rather than moving the timestamp.
        notification.MarkRead(now);

        await notifications.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
