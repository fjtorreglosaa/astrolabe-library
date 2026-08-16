using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Infrastructure.Persistence;

/// <summary>
/// Shared implementation of <see cref="IUnitOfWork"/> for every bounded context's unit of work.
///
/// Each context's unit of work delegates here rather than reimplementing commit and transaction
/// handling, so there is exactly one place where "commit" means something.
/// </summary>
public abstract class UnitOfWorkBase(AstrolabeDbContext context) : IUnitOfWork
{
    protected AstrolabeDbContext Context { get; } = context;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        Context.SaveChangesAsync(cancellationToken);

    public Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default) =>
        Context.ExecuteInTransactionAsync(operation, cancellationToken);

    public Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default) =>
        Context.ExecuteInTransactionAsync(operation, cancellationToken);
}
