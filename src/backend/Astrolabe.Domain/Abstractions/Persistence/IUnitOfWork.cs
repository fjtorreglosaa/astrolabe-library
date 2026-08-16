namespace Astrolabe.Domain.Abstractions.Persistence;

/// <summary>
/// Coordinates persistence so a multi-step business operation commits atomically.
/// See GUIDELINES.md section 15.
///
/// <para>
/// Every repository resolved within a request shares this unit of work's change tracker, so their
/// staged changes commit or roll back together. That is what <see cref="SaveChangesAsync"/> means:
/// one call, one transaction, for everything staged since the last one.
/// </para>
///
/// <para>
/// It deliberately does <b>not</b> expose repositories. See the decision log in
/// <c>global_tech_spec.md</c> section 4.
/// </para>
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Commits everything staged. EF Core already wraps a single call in a transaction, so an
    /// operation that stages all its work before one call needs nothing further.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs work inside an explicit transaction, committing on success and rolling back on any
    /// exception.
    ///
    /// <para>
    /// Needed only when an operation cannot stage all its changes before saving — for example when
    /// it must read back a generated value, or call <see cref="SaveChangesAsync"/> more than once.
    /// Reach for it deliberately: the common case does not need it, and holding a transaction open
    /// across an external call is how deadlocks start.
    /// </para>
    /// </summary>
    Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default);

    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}
