using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Notifications.Repositories;

public interface INotificationsUnitOfWork : IUnitOfWork
{
    INotificationRepository Notifications { get; }

    INotificationPreferenceRepository Preferences { get; }
}
