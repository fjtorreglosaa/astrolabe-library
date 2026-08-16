using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Reservations.Queries.GetMyReservations;

/// <summary>
/// The caller's own loans. Implements BR-RSV-021.
///
/// There is no member parameter, so leaking somebody else's loans is not a check that can be
/// forgotten — it is a thing the contract cannot express.
/// </summary>
public sealed record GetMyReservationsQuery(
    ReservationStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReservationDto>>;
