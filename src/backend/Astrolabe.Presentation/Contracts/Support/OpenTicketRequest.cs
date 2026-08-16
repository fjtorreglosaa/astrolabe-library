using Astrolabe.Domain.Features.Support.Enums;

namespace Astrolabe.Presentation.Contracts.Support;

/// <summary>A new ticket. The member comes from the token, never from the payload.</summary>
public sealed record OpenTicketRequest(
    string Subject, string Body, TicketCategory Category, Guid LibraryId);
