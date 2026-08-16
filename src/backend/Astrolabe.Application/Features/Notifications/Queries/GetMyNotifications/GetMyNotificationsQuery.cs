using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Notifications;

namespace Astrolabe.Application.Features.Notifications.Queries.GetMyNotifications;

/// <summary>
/// The caller's own centre. BR-NTF-007 is enforced by the shape: there is no parameter for whose.
/// </summary>
public sealed record GetMyNotificationsQuery(int Limit = 30) : IQuery<NotificationFeedDto>;
