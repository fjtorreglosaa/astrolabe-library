namespace Astrolabe.Domain.Abstractions;

/// <summary>
/// Base type for domain entities. Identity is the only thing this base provides; behaviour and
/// invariants belong to the concrete entity. See GUIDELINES.md section 11.
/// </summary>
public abstract class Entity
{
    protected Entity(Guid id) => Id = id;

    /// <summary>Parameterless constructor required by the ORM. Never call it from domain code.</summary>
    protected Entity()
    {
    }

    public Guid Id { get; protected init; }
}
