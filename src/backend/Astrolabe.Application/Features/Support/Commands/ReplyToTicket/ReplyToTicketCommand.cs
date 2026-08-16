using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Support.Commands.ReplyToTicket;

/// <summary>
/// Adds a message. Implements BR-SUP-008, BR-SUP-011 and BR-SUP-012.
///
/// Whether the author is the member or an agent is decided from the caller's role, never from the
/// payload — otherwise a member could post as staff.
/// </summary>
public sealed record ReplyToTicketCommand(Guid TicketId, string Text) : ICommand;
