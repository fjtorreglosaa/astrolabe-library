using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Support.Commands.TransitionTicket;

/// <summary>Assigns, resolves or reopens a ticket. Staff only, and scoped by BR-SUP-010.</summary>
public sealed record TransitionTicketCommand(
    Guid TicketId, TicketTransition Transition) : ICommand;
