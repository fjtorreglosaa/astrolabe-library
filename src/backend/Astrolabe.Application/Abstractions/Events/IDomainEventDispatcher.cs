using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Application.Abstractions.Events;

/// <summary>
/// Publishes domain events to whatever reacts to them.
///
/// Dispatch happens <b>after</b> the unit of work commits, so no reaction can observe a change that
/// was later rolled back. The corollary is that a failing reaction cannot undo the commit — a
/// reaction must therefore be something the system can retry or survive losing, never a step the
/// business outcome depends on.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
