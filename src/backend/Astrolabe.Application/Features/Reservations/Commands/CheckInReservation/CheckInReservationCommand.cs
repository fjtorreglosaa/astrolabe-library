using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Reservations.Commands.CheckInReservation;

/// <summary>
/// The library's half: staff physically hold the copy. Implements BR-RSV-016 to BR-RSV-020.
/// The only act that puts a volume back on the shelf.
/// </summary>
public sealed record CheckInReservationCommand(Guid ReservationId) : ICommand;
