using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Queries.GetLibraryReservations;

/// <summary>
/// The desk's view. Implements BR-RSV-022: scoped to the libraries assigned to the caller, and
/// unrestricted for a super administrator.
/// </summary>
public sealed record GetLibraryReservationsQuery(
    ReservationStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<StaffReservationDto>>;
