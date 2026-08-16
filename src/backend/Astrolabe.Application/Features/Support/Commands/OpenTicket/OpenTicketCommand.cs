using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Support;
using Astrolabe.Domain.Features.Support.Enums;

namespace Astrolabe.Application.Features.Support.Commands.OpenTicket;

/// <summary>
/// A member opens a ticket. Implements BR-SUP-001 and BR-SUP-009.
///
/// The body is required alongside the subject: a ticket with a title and no question is one somebody
/// has to chase before they can answer it.
/// </summary>
public sealed record OpenTicketCommand(
    string Subject,
    string Body,
    TicketCategory Category,
    Guid LibraryId) : ICommand<TicketDto>;
