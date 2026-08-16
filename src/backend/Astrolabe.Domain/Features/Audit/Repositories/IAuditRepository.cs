using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Audit.Entities;

namespace Astrolabe.Domain.Features.Audit.Repositories;

/// <summary>
/// Persistence for <see cref="AuditEntry"/>. Append-only by design: the contract exposes no way to
/// modify or remove an entry beyond what the generic base requires.
/// </summary>
public interface IAuditRepository : IRepository<AuditEntry>
{
    Task<IReadOnlyList<AuditEntry>> GetForSubjectAsync(
        Guid subjectUserId, int limit, CancellationToken cancellationToken = default);
}
