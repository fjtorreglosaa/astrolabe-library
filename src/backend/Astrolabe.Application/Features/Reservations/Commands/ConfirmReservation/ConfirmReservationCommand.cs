using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Domain.Features.Reservations.Enums;

namespace Astrolabe.Application.Features.Reservations.Commands.ConfirmReservation;

/// <summary>
/// Takes a copy for the caller. Implements BR-RSV-001 to BR-RSV-008.
///
/// The member is never a parameter: BR-RSV-021 becomes structural rather than a check somebody has
/// to remember. <paramref name="IdempotencyKey"/> is optional but strongly advised — without it a
/// retried request takes a second copy.
/// </summary>
public sealed record ConfirmReservationCommand(
    Guid BookId,
    Guid LibraryId,
    DeliveryMethod Delivery,
    string? IdempotencyKey) : ICommand<ReservationDto>;
