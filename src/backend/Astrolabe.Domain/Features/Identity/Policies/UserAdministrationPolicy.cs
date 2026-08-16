using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Identity.Policies;

/// <summary>
/// Who may block, restore or delete whom from the user directory.
///
/// <para>
/// Transcribed from the prototype's <c>canManage</c>, which is the authority on this and states it
/// exactly: <c>!isSelf &amp;&amp; (isSuper ? !targetSuper : !targetStaff)</c>. Three refusals, each
/// with its own reason, because a console that greys out a button without saying why is a console
/// people file tickets about.
/// </para>
/// <para>
/// A pure decision over two roles and two identifiers, so every branch is testable without a
/// database and no handler can reach the answer another way.
/// </para>
/// </summary>
public static class UserAdministrationPolicy
{
    /// <summary>
    /// Whether <paramref name="actorRole"/> may administer the target account.
    /// </summary>
    public static Result EnsureCanAdminister(
        Guid actorId, UserRole actorRole, Guid targetId, UserRole targetRole)
    {
        // Ahead of every other check. Blocking yourself is the one mistake in this console that
        // cannot be undone from inside it — you would be locked out of the screen that unblocks you.
        if (actorId == targetId)
        {
            return Result.Failure(IdentityErrors.CannotAdministerYourself);
        }

        if (!actorRole.IsStaff())
        {
            return Result.Failure(IdentityErrors.StaffRequired);
        }

        // Not even a super administrator, and deliberately: BR-NET-012 keeps the network from ever
        // being left without one, and a console where two super administrators can lock each other
        // out is a console where the network can be left with none.
        if (targetRole is UserRole.SuperAdmin)
        {
            return Result.Failure(IdentityErrors.CannotAdministerASuperAdmin);
        }

        // BR-NET-008 reserves creating and revoking administrators to a super administrator, so an
        // administrator must not be able to reach one sideways through the directory either.
        if (targetRole.IsStaff() && actorRole is not UserRole.SuperAdmin)
        {
            return Result.Failure(IdentityErrors.SuperAdminRequiredForStaff);
        }

        return Result.Success();
    }

    /// <summary>
    /// The same question without the reason, for a list projection that has to decide whether to
    /// offer the actions at all. The handler still calls <see cref="EnsureCanAdminister"/> before
    /// acting — this only decides what a screen draws.
    /// </summary>
    public static bool CanAdminister(
        Guid actorId, UserRole actorRole, Guid targetId, UserRole targetRole) =>
        EnsureCanAdminister(actorId, actorRole, targetId, targetRole).IsSuccess;
}
