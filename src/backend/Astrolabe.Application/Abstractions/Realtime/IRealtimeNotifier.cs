using Astrolabe.Application.Contracts.Realtime;

namespace Astrolabe.Application.Abstractions.Realtime;

/// <summary>
/// Pushes a change to whoever is watching a screen it affects.
/// </summary>
/// <remarks>
/// <para>
/// <b>Delivery is not guaranteed and nothing may depend on it.</b> A member with no browser open has
/// no connection, a reconnect drops whatever was in flight, and the implementation swallows its own
/// failures on purpose — a hub that is down must never fail the transaction that succeeded. This is
/// the same reason a domain event handler may not carry a business outcome: it runs after the commit
/// and may be lost. Real-time is how a screen finds out sooner, never how it finds out at all.
/// </para>
/// <para>
/// The two audiences are separate methods rather than one method with a target, so that choosing who
/// sees a change is a decision made in the open at every call site. Broadcasting a member's fine to
/// staff is the kind of mistake a <c>target</c> parameter makes easy to commit and hard to notice.
/// </para>
/// </remarks>
public interface IRealtimeNotifier
{
    /// <summary>
    /// Tells one member. Reaches every device they have open and nobody else.
    /// </summary>
    Task NotifyMemberAsync(Guid memberId, RealtimeEvent @event, CancellationToken cancellationToken = default);

    /// <summary>
    /// Tells every signed-in administrator.
    /// </summary>
    /// <remarks>
    /// Deliberately not scoped by library. The administration screens are already city-scoped by
    /// their query handlers, so this is a prompt to refetch and the refetch returns only what the
    /// caller may see. Scoping the signal as well would put the same authority rule in two places,
    /// and the copy without a database behind it would be the one that drifted.
    /// </remarks>
    Task NotifyStaffAsync(RealtimeEvent @event, CancellationToken cancellationToken = default);
}
