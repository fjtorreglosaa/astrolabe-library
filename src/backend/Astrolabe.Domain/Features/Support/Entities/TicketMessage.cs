using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Features.Support.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Support.Entities;

/// <summary>
/// One entry in a conversation. Implements BR-SUP-008.
///
/// <para>
/// There is no method here that changes anything. A support conversation is a record of what was
/// said, and an editable one is a record of what somebody would prefer to have said.
/// </para>
/// <para>
/// <see cref="AuthorName"/> is stored rather than resolved from the account. An agent revoked next
/// year still wrote this line, and a conversation that forgets who answered is one nobody can audit.
/// </para>
/// </summary>
public sealed class TicketMessage : Entity
{
    public const int MaxTextLength = 4000;

    private TicketMessage()
    {
    }

    private TicketMessage(
        Guid id, Guid authorUserId, TicketAuthor author, string authorName,
        string text, DateTimeOffset now) : base(id)
    {
        AuthorUserId = authorUserId;
        Author = author;
        AuthorName = authorName;
        Text = text;
        WrittenAt = now;
    }

    public Guid AuthorUserId { get; private set; }

    public TicketAuthor Author { get; private set; }

    public string AuthorName { get; private set; } = string.Empty;

    public string Text { get; private set; } = string.Empty;

    public DateTimeOffset WrittenAt { get; private set; }

    public static Result<TicketMessage> Write(
        Guid authorUserId, TicketAuthor author, string authorName,
        string text, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Result.Failure<TicketMessage>(SupportErrors.MessageRequired);
        }

        var trimmed = text.Trim();

        return Result.Success(new TicketMessage(
            Guid.NewGuid(), authorUserId, author,
            string.IsNullOrWhiteSpace(authorName) ? "Unknown" : authorName.Trim(),
            trimmed.Length > MaxTextLength ? trimmed[..MaxTextLength] : trimmed,
            now));
    }
}
