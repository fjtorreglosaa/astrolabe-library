using Astrolabe.Domain.Features.Audit.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Audit;

/// <summary>
/// Composes the audit repository over the shared context, so an entry commits with the change it
/// describes rather than in a transaction of its own.
/// </summary>
public sealed class AuditUnitOfWork(
    AstrolabeDbContext context,
    IAuditRepository entries) : UnitOfWorkBase(context), IAuditUnitOfWork
{
    public IAuditRepository Entries { get; } = entries;
}
