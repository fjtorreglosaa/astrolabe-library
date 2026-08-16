namespace Astrolabe.Domain.Features.Support.Enums;

/// <summary>Who wrote a message. Stored rather than derived, because an agent may later be revoked.</summary>
public enum TicketAuthor
{
    Member = 0,
    Agent = 1
}
