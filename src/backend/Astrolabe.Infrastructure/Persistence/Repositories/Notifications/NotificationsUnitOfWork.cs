using Astrolabe.Domain.Features.Notifications.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Notifications;

public sealed class NotificationsUnitOfWork(
    AstrolabeDbContext context,
    INotificationRepository notifications,
    INotificationPreferenceRepository preferences)
    : UnitOfWorkBase(context), INotificationsUnitOfWork
{
    public INotificationRepository Notifications { get; } = notifications;

    public INotificationPreferenceRepository Preferences { get; } = preferences;
}
