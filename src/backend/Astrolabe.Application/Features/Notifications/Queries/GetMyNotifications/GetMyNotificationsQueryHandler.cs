using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Notifications;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Notifications.Policies;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Notifications.Queries.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler(
    INotificationsUnitOfWork notifications,
    ICurrentUser currentUser) : IQueryHandler<GetMyNotificationsQuery, NotificationFeedDto>
{
    public async Task<Result<NotificationFeedDto>> Handle(
        GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<NotificationFeedDto>(IdentityErrors.InvalidCredentials);
        }

        var items = await notifications.Notifications.GetForMemberAsync(
            memberId, request.Limit, cancellationToken);

        // BR-NTF-010. Counted, not stored, and counted over everything rather than over the page —
        // a badge that only counted the first thirty would quietly stop growing.
        var unread = await notifications.Notifications.CountUnreadAsync(memberId, cancellationToken);

        var muted = await notifications.Preferences.GetMutedAsync(memberId, cancellationToken);

        return Result.Success(new NotificationFeedDto(
            unread,
            [.. muted.Select(family => family.ToString())],
            [.. items.Select(item => new NotificationDto(
                item.Id,
                item.Kind.ToString(),
                NotificationFamilies.Of(item.Kind).ToString(),
                item.Title,
                item.Body,
                item.Route,
                item.OccurredAt,
                item.IsRead))]));
    }
}
