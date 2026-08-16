namespace Astrolabe.Application.Contracts.Support;

/// <summary>
/// One ticket with its conversation.
///
/// <c>CanReply</c> and <c>CanRate</c> are decided server-side: both depend on the ticket's status and
/// on who is asking, and a screen that worked either out itself would be a second copy of BR-SUP-005
/// and BR-SUP-011.
/// </summary>
public sealed record TicketDto(
    Guid Id,
    string Reference,
    string Subject,
    string Category,
    string Status,
    string? AgentName,
    string LibraryName,
    string MemberName,
    int? Rating,
    string? Review,
    bool CanReply,
    bool CanRate,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<TicketMessageDto> Messages);
