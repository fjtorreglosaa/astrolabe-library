using System.Linq.Expressions;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Abstractions.Persistence;

/// <summary>
/// The generic persistence contract every repository shares.
///
/// It declares only operations that are genuinely identical for every entity. Anything that depends
/// on what the entity <em>means</em> belongs on the concrete interface that extends this one, so the
/// base never becomes a dumping ground. See GUIDELINES.md section 14.
///
/// <para>
/// Predicates are <see cref="Expression{TDelegate}"/> rather than <see cref="Func{T, TResult}"/> so
/// the provider can translate them into SQL. A <c>Func</c> would compile to a delegate, forcing the
/// whole table into memory before filtering. <see cref="System.Linq.Expressions"/> ships with the
/// runtime, so this keeps the Domain layer free of external packages.
/// </para>
///
/// <para>
/// Reads return **tracked** entities. An entity you read can therefore be mutated and saved, which is
/// the safe default: returning untracked entities that a caller then modifies and expects to persist
/// is a silent data-loss bug. Read-only projections belong on concrete repositories, which have the
/// untracked query available to them.
/// </para>
/// </summary>
/// <typeparam name="TEntity">The entity this repository persists.</typeparam>
public interface IRepository<TEntity>
    where TEntity : Entity
{
    // ---------- Reads: single ----------

    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// The first entity matching the predicate, or null. Use when the predicate is expected to match
    /// at most one row; it does not fail when several match.
    /// </summary>
    Task<TEntity?> GetByFilterAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    // ---------- Reads: many ----------

    /// <summary>
    /// Every row. Only safe for bounded reference data such as countries or libraries. For anything
    /// that grows with usage, use <see cref="GetPagedAsync"/> instead.
    /// </summary>
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> GetByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>All entities matching the predicate. Bounded sets only — otherwise use paging.</summary>
    Task<IReadOnlyList<TEntity>> ListByFilterAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// A page of results with its total count. The predicate is optional so an unfiltered listing
    /// still cannot escape paging.
    /// </summary>
    Task<PagedResult<TEntity>> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // ---------- Aggregates ----------

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>Counts matching rows without materialising them.</summary>
    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default);

    // ---------- Writes ----------

    /// <summary>
    /// Stages one entity for insertion.
    ///
    /// Asynchronous because a provider may need to reach the database to generate a key. With the
    /// client-generated identifiers this system uses it completes synchronously, but the signature
    /// keeps the contract honest if a sequence-backed key is ever introduced. Nothing is written
    /// until <see cref="IUnitOfWork.SaveChangesAsync"/> commits.
    /// </summary>
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>Stages many entities for insertion, in one round trip.</summary>
    Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a detached entity as modified. Unnecessary for an entity obtained from this repository,
    /// which is already tracked — calling it then is harmless but redundant.
    /// </summary>
    void Update(TEntity entity);

    void Remove(TEntity entity);

    void RemoveRange(IEnumerable<TEntity> entities);
}
