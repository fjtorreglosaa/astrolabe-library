namespace Astrolabe.Application.Contracts.Support;

/// <summary>A row in either list — the member's own, or the staff queue.</summary>
public sealed record TicketSummaryDto(
    Guid Id,
    string Reference,
    string Subject,
    string Category,
    string Status,
    string? AgentName,
    string LibraryName,
    string MemberName,
    int? Rating,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
