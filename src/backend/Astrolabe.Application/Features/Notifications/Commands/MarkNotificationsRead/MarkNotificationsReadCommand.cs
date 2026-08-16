using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Notifications.Commands.MarkNotificationsRead;

/// <summary>
/// Marks one notification read, or all of them. Implements BR-NTF-006 and BR-NTF-007.
///
/// One command with an optional identifier rather than two, because "mark this" and "mark all" share
/// the ownership check and differ by a `where` clause. Two handlers would be two places for
/// BR-NTF-007 to be forgotten in.
/// </summary>
/// <param name="NotificationId">Null marks every unread notification the caller owns.</param>
public sealed record MarkNotificationsReadCommand(Guid? NotificationId = null) : ICommand;
