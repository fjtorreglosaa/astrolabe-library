using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Notifications.Enums;

namespace Astrolabe.Application.Features.Notifications.Commands.SetNotificationPreference;

/// <summary>
/// Mutes or unmutes one family. Implements BR-NTF-002 and BR-NTF-003.
/// </summary>
/// <param name="Muted">
/// True stores a row, false deletes it. Absence of a row is "on", so unmuting genuinely removes the
/// decision rather than recording a second one.
/// </param>
public sealed record SetNotificationPreferenceCommand(
    NotificationFamily Family, bool Muted) : ICommand;
