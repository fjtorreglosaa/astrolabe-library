using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Support.Commands.RateTicket;

/// <summary>
/// The member's verdict. Implements BR-SUP-005 and BR-SUP-006.
///
/// No member identifier: it comes from the token, and BR-SUP-005 says only their own.
/// </summary>
public sealed record RateTicketCommand(Guid TicketId, int Stars, string? Review) : ICommand;
