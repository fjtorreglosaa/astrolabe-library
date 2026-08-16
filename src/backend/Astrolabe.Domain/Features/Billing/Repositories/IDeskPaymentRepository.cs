using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Billing.Repositories;

/// <summary>Persistence for <see cref="DeskPayment"/>.</summary>
public interface IDeskPaymentRepository : IRepository<DeskPayment>
{
    Task<DeskPayment?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>The desk queue, scoped to the libraries the caller may act on. BR-BIL-005.</summary>
    Task<PagedResult<DeskPayment>> GetForLibrariesAsync(
        IReadOnlyCollection<Guid> libraryIds, DeskPaymentStatus? status,
        int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DeskPayment>> GetForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default);
}
