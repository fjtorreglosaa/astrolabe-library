using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Network;

public sealed class AdminInvitationRepository(AstrolabeDbContext context)
    : Repository<AdminInvitation>(context), IAdminInvitationRepository
{
    public async Task<AdminInvitation?> GetPendingByTokenHashAsync(
        byte[] tokenHash, CancellationToken cancellationToken = default) =>
        await Query.FirstOrDefaultAsync(
            i => i.TokenHash == tokenHash && i.AcceptedAt == null && i.RevokedAt == null,
            cancellationToken);

    public async Task<IReadOnlyList<AdminInvitation>> GetPendingByUserAsync(
        Guid userId, CancellationToken cancellationToken = default) =>
        await Query
            .Where(i => i.UserId == userId && i.AcceptedAt == null && i.RevokedAt == null)
            .ToListAsync(cancellationToken);
}
