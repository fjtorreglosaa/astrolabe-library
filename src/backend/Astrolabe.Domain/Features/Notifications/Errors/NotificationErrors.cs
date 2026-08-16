using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Notifications.Errors;

public static class NotificationErrors
{
    public static readonly Error NotFound =
        Error.NotFound("notifications.not_found", "That notification does not exist.");

    /// <summary>
    /// BR-NTF-007. Not "not found", although it would be tempting: the caller is authenticated and
    /// acting on their own centre, so an authorization answer is the honest one.
    /// </summary>
    public static readonly Error NotYours =
        Error.Authorization("notifications.not_yours", "That notification is not yours.");

    public static readonly Error TitleRequired =
        Error.Validation("notifications.title_required", "A notification must say what happened.");
}
