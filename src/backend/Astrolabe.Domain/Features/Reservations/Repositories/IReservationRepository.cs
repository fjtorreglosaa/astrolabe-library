using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Reservations.Repositories;

/// <summary>Persistence for <see cref="Reservation"/>.</summary>
public interface IReservationRepository : IRepository<Reservation>
{
    /// <summary>
    /// A member's reservations, newest first.
    ///
    /// Takes the member identifier because the repository serves both the member's own listing and
    /// the staff one; BR-RSV-021 is enforced at the query, which is the only place that knows who is
    /// asking.
    /// </summary>
    Task<PagedResult<Reservation>> GetForMemberAsync(
        Guid memberId, ReservationStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>Backs BR-RSV-007. Two active reservations of one physical copy make no sense.</summary>
    Task<bool> HasActiveForCopyAsync(
        Guid memberId, Guid bookCopyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Backs BR-RSV-008. A replayed confirmation finds its own first attempt rather than taking a
    /// second copy.
    /// </summary>
    Task<Reservation?> GetByIdempotencyKeyAsync(
        Guid memberId, string idempotencyKey, CancellationToken cancellationToken = default);

    /// <summary>Every active reservation of a member, for the dashboard's counts.</summary>
    Task<IReadOnlyList<Reservation>> GetActiveForMemberAsync(
        Guid memberId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returned reservations that came back late. The sweep in <c>billing</c> uses this to find
    /// loans whose fine was never assessed, so a lost domain event never becomes an unbilled fine.
    /// </summary>
    Task<IReadOnlyList<Reservation>> GetLateReturnsAsync(
        int maxCount, CancellationToken cancellationToken = default);

    /// <summary>Reservations against a set of libraries. Empty set yields an empty page.</summary>
    Task<PagedResult<Reservation>> GetForLibrariesAsync(
        IReadOnlyCollection<Guid> libraryIds, ReservationStatus? status, int page, int pageSize,
        CancellationToken cancellationToken = default);
}
