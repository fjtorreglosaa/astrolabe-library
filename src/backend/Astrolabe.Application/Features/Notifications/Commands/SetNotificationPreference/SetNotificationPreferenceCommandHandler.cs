using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Notifications.Entities;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Notifications.Commands.SetNotificationPreference;

public sealed class SetNotificationPreferenceCommandHandler(
    INotificationsUnitOfWork notifications,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<SetNotificationPreferenceCommand>
{
    public async Task<Result> Handle(
        SetNotificationPreferenceCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure(IdentityErrors.InvalidCredentials);
        }

        var existing = await notifications.Preferences.GetAsync(
            memberId, request.Family, cancellationToken);

        // Idempotent both ways: muting something already muted, or unmuting something that was never
        // muted, is a no-op rather than a duplicate row or a failure.
        if (request.Muted && existing is null)
        {
            await notifications.Preferences.AddAsync(
                NotificationPreference.Mute(memberId, request.Family, clock.UtcNow),
                cancellationToken);
        }
        else if (!request.Muted && existing is not null)
        {
            notifications.Preferences.Remove(existing);
        }

        await notifications.SaveChangesAsync(cancellationToken);

        // BR-NTF-004 is satisfied by omission: nothing here touches a notification already
        // delivered. Muting changes what arrives next, never what already did.
        return Result.Success();
    }
}
