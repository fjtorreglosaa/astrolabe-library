namespace Astrolabe.Domain.Abstractions;

/// <summary>
/// An entity that is the entry point to a consistency boundary, and the only kind of entity that may
/// raise domain events. Events are collected here and dispatched after the unit of work commits, so
/// no handler ever observes a change that was later rolled back.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot(Guid id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
