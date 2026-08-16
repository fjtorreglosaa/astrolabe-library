using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Identity;

public sealed class AuditRepository(AstrolabeDbContext context)
    : Repository<AuditEntry>(context), IAuditRepository
{
    public async Task<IReadOnlyList<AuditEntry>> GetForSubjectAsync(
        Guid subjectUserId, int limit, CancellationToken cancellationToken = default) =>
        await ReadOnlyQuery
            .Where(a => a.SubjectUserId == subjectUserId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit < 1 ? 1 : limit)
            .ToListAsync(cancellationToken);
}
