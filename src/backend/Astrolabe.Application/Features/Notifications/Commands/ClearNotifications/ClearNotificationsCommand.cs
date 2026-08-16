using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Notifications.Commands.ClearNotifications;

/// <summary>
/// Empties the caller's centre. Implements BR-NTF-008.
///
/// Permanent, and no undo is offered anywhere — the prototype's "Clear all" does not have one, and
/// inventing a recycle bin for messages about things that already happened would be inventing
/// product.
/// </summary>
public sealed record ClearNotificationsCommand : ICommand;
