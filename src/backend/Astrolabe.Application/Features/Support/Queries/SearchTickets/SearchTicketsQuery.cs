using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Support;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Support.Queries.SearchTickets;

/// <summary>
/// Tickets the caller may see. One query for both audiences, because the difference is entirely a
/// filter the handler applies — a member sees their own, staff see their libraries'.
///
/// There is no parameter for whose. That is what makes BR-SUP-004 a property of the shape rather
/// than a check somebody has to remember.
/// </summary>
public sealed record SearchTicketsQuery(
    string? Term,
    TicketStatus? Status,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<TicketSummaryDto>>;
