using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Network.Entities;

namespace Astrolabe.Domain.Features.Network.Repositories;

/// <summary>Persistence for <see cref="AdminInvitation"/>.</summary>
public interface IAdminInvitationRepository : IRepository<AdminInvitation>
{
    /// <summary>Looked up by hash: the plaintext token is never stored, so it cannot be searched for.</summary>
    Task<AdminInvitation?> GetPendingByTokenHashAsync(
        byte[] tokenHash, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminInvitation>> GetPendingByUserAsync(
        Guid userId, CancellationToken cancellationToken = default);
}
