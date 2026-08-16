using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Reservations;
using Astrolabe.Domain.Features.Reservations.Enums;

namespace Astrolabe.Application.Features.Reservations.Queries.QuoteReservation;

/// <summary>
/// What reserving would cost and when it would be due, with every branch and its verdict.
///
/// Exists so the confirmation modal can show the fee, the due date and which copies are closed to
/// this member — before anything is committed and before any stock moves.
/// </summary>
public sealed record QuoteReservationQuery(
    Guid BookId,
    DeliveryMethod Delivery) : IQuery<ReservationQuoteDto>;
