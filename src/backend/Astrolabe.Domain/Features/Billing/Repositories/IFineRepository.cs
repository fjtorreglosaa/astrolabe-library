using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;

namespace Astrolabe.Domain.Features.Billing.Repositories;

/// <summary>Persistence for <see cref="Fine"/>.</summary>
public interface IFineRepository : IRepository<Fine>
{
    /// <summary>Backs BR-BIL-010. The unique index is the real guard; this gives a clean answer first.</summary>
    Task<Fine?> GetByReservationAsync(Guid reservationId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Fine>> GetForMemberAsync(
        Guid memberId, FineStatus? status, CancellationToken cancellationToken = default);

    /// <summary>The specific fines a payment or a desk code covers, loaded together.</summary>
    Task<IReadOnlyList<Fine>> GetByIdsForMemberAsync(
        Guid memberId, IReadOnlyCollection<Guid> fineIds, CancellationToken cancellationToken = default);

    /// <summary>Every fine held by one desk code, for validation and for release.</summary>
    Task<IReadOnlyList<Fine>> GetByDeskPaymentAsync(
        Guid deskPaymentId, CancellationToken cancellationToken = default);
}
