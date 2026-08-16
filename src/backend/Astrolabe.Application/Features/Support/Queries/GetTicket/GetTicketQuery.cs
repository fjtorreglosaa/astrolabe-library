using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Support;

namespace Astrolabe.Application.Features.Support.Queries.GetTicket;

/// <summary>One ticket with its conversation. BR-SUP-004 decides who may read it.</summary>
public sealed record GetTicketQuery(Guid TicketId) : IQuery<TicketDto>;
