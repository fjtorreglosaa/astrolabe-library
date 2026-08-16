namespace Astrolabe.Application.Contracts.Support;

/// <summary>One entry in a conversation. Never edited, so nothing here is nullable for a draft.</summary>
public sealed record TicketMessageDto(
    Guid Id, string Author, string AuthorName, string Text, DateTimeOffset WrittenAt);
