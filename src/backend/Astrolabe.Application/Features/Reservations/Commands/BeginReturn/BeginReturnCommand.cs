using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Reservations.Enums;

namespace Astrolabe.Application.Features.Reservations.Commands.BeginReturn;

/// <summary>
/// The member's half of the return: they handed the copy over and prove it with the code the courier
/// or librarian read out. Implements BR-RSV-013 to BR-RSV-015.
///
/// Separate from the check-in on purpose. A member cannot make a copy appear at the desk by pressing
/// a button, and merging the two would put them one boolean away from completing their own return.
/// </summary>
public sealed record BeginReturnCommand(
    Guid ReservationId,
    ReturnMethod Method,
    string Code) : ICommand;
