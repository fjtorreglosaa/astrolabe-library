using Astrolabe.Domain.Features.Notifications.Enums;

namespace Astrolabe.Application.Abstractions.Notifications;

/// <summary>
/// Creates a notification, unless the member has muted its family. Implements BR-NTF-003.
///
/// <para>
/// One seam because six event handlers need the same mute check, and six copies of a check is five
/// chances to forget it. It is also the reason BR-NTF-003 can say "produce no notification at all"
/// rather than "hide it on read": there is exactly one place that decides, and it decides before
/// anything is written.
/// </para>
/// <para>
/// Never throws. Every caller is a domain event handler, and BR-NTF-005 forbids a notification from
/// being able to fail the outcome that caused it.
/// </para>
/// </summary>
public interface INotificationRaiser
{
    Task RaiseAsync(
        Guid memberId,
        NotificationKind kind,
        string title,
        string body,
        string? route = null,
        CancellationToken cancellationToken = default);
}
