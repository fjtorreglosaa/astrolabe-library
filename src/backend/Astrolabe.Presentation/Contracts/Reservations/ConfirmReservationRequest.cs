using Astrolabe.Domain.Features.Reservations.Enums;

namespace Astrolabe.Presentation.Contracts.Reservations;

/// <summary>
/// The body of a confirmation. The member comes from the token, never from the payload.
///
/// <c>IdempotencyKey</c> is the client's to generate, once per attempt the member makes — not once
/// per HTTP request. That is what makes a retry safe and a second deliberate reservation possible.
/// </summary>
public sealed record ConfirmReservationRequest(
    Guid BookId,
    Guid LibraryId,
    DeliveryMethod Delivery,
    string? IdempotencyKey);
