using Astrolabe.Application.Contracts.Support;
using Astrolabe.Domain.Features.Support.Entities;
using Astrolabe.Domain.Features.Support.Enums;

namespace Astrolabe.Application.Shared.Support;

/// <summary>
/// Turns a ticket into what each audience sees.
///
/// <para>
/// <c>CanReply</c> and <c>CanRate</c> are computed here, once, from the same rules the handlers
/// enforce. A screen deciding for itself would be a second copy of BR-SUP-005 and BR-SUP-011, and
/// the day they disagree the member is offered a button that fails.
/// </para>
/// </summary>
public static class TicketProjection
{
    /// <summary>The prototype's own category labels, verbatim.</summary>
    public static string Label(TicketCategory category) => category switch
    {
        TicketCategory.PaymentsAndFines => "Payments and fines",
        TicketCategory.ReservationsAndReturns => "Reservations and returns",
        TicketCategory.CatalogueAndAvailability => "Catalogue and availability",
        TicketCategory.AccountAndPlan => "Account and plan",
        _ => "Something is broken"
    };

    public static string Label(TicketStatus status) => status switch
    {
        TicketStatus.Created => "Created",
        TicketStatus.InReview => "In review",
        _ => "Resolved"
    };

    public static TicketSummaryDto ToSummary(Ticket ticket, string libraryName, string memberName) =>
        new(ticket.Id, ticket.Reference, ticket.Subject, Label(ticket.Category),
            Label(ticket.Status), ticket.AgentName, libraryName, memberName,
            ticket.Rating, ticket.CreatedAt, ticket.UpdatedAt);

    public static TicketDto ToDetail(
        Ticket ticket, string libraryName, string memberName, bool viewerIsTheMember) =>
        new(ticket.Id,
            ticket.Reference,
            ticket.Subject,
            Label(ticket.Category),
            Label(ticket.Status),
            ticket.AgentName,
            libraryName,
            memberName,
            ticket.Rating,
            ticket.Review,
            // BR-SUP-011. Resolved admits nothing from either side until it is reopened.
            CanReply: ticket.Status is not TicketStatus.Resolved,
            // BR-SUP-005. Only the member, only once resolved. Staff never rate their own work.
            CanRate: viewerIsTheMember && ticket.Status is TicketStatus.Resolved,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            [.. ticket.Messages
                .OrderBy(message => message.WrittenAt)
                .Select(message => new TicketMessageDto(
                    message.Id, message.Author.ToString(), message.AuthorName,
                    message.Text, message.WrittenAt))]);
}
