using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Support.Enums;
using Astrolabe.Domain.Features.Support.Errors;
using Astrolabe.Domain.Features.Support.Events;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Support.Entities;

/// <summary>
/// One member's issue and the conversation that answers it. Implements BR-SUP-001 to BR-SUP-012.
///
/// <para>
/// The conversation is owned rather than a separate aggregate: a message has no life without its
/// ticket, is never queried alone, and the transition rules read the list to decide. Splitting them
/// would need a transaction across two aggregates to append one line.
/// </para>
/// </summary>
public sealed class Ticket : AggregateRoot
{
    public const int MaxSubjectLength = 200;
    public const int MaxReviewLength = 500;

    private readonly List<TicketMessage> _messages = [];

    private Ticket()
    {
    }

    private Ticket(
        Guid id, string reference, Guid memberId, TicketCategory category, Guid libraryId,
        string subject, TicketMessage first, DateTimeOffset now) : base(id)
    {
        Reference = reference;
        MemberId = memberId;
        Category = category;
        LibraryId = libraryId;
        Subject = subject;
        Status = TicketStatus.Created;
        CreatedAt = now;
        UpdatedAt = now;

        _messages.Add(first);
    }

    /// <summary>
    /// `TCK-NNNN`. What a member quotes on the phone, which is why it is not the identifier — a GUID
    /// is unreadable aloud, and this is the one place readability matters most.
    /// </summary>
    public string Reference { get; private set; } = string.Empty;

    public Guid MemberId { get; private set; }

    public TicketCategory Category { get; private set; }

    /// <summary>BR-SUP-009. Which library's staff can act on it.</summary>
    public Guid LibraryId { get; private set; }

    public string Subject { get; private set; } = string.Empty;

    public TicketStatus Status { get; private set; }

    public Guid? AgentUserId { get; private set; }

    public string? AgentName { get; private set; }

    public int? Rating { get; private set; }

    public string? Review { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<TicketMessage> Messages => _messages;

    public static Result<Ticket> Open(
        string reference,
        Guid memberId,
        TicketCategory category,
        Guid libraryId,
        string subject,
        string body,
        string memberName,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<Ticket>(SupportErrors.SubjectRequired);
        }

        // The opening message is part of opening. A ticket with a subject and no body would be a
        // question nobody can answer, and letting it exist means somebody has to chase it.
        var first = TicketMessage.Write(memberId, TicketAuthor.Member, memberName, body, now);

        if (first.IsFailure)
        {
            return Result.Failure<Ticket>(first.Error);
        }

        var trimmed = subject.Trim();

        return Result.Success(new Ticket(
            Guid.NewGuid(), reference, memberId, category, libraryId,
            trimmed.Length > MaxSubjectLength ? trimmed[..MaxSubjectLength] : trimmed,
            first.Value, now));
    }

    /// <summary>
    /// BR-SUP-003. Assigning is what moves a ticket into review — the two are one act, because a
    /// ticket in review with nobody on it is a ticket everybody assumes somebody else has.
    /// </summary>
    public Result Assign(Guid agentUserId, string agentName, DateTimeOffset now)
    {
        if (Status is TicketStatus.Resolved)
        {
            return Result.Failure(SupportErrors.TicketIsResolved);
        }

        AgentUserId = agentUserId;
        AgentName = agentName;
        Status = TicketStatus.InReview;
        UpdatedAt = now;

        return Result.Success();
    }

    public Result Reply(
        Guid authorUserId, TicketAuthor author, string authorName, string text, DateTimeOffset now)
    {
        // BR-SUP-011. A resolved ticket admits nothing until somebody deliberately reopens it.
        if (Status is TicketStatus.Resolved)
        {
            return Result.Failure(SupportErrors.TicketIsResolved);
        }

        var message = TicketMessage.Write(authorUserId, author, authorName, text, now);

        if (message.IsFailure)
        {
            return Result.Failure(message.Error);
        }

        _messages.Add(message.Value);
        UpdatedAt = now;

        // BR-SUP-012, and only for an agent's reply: a member is not notified about their own words.
        if (author is TicketAuthor.Agent)
        {
            Raise(new TicketAnswered(Guid.NewGuid(), now, Id, MemberId, Reference, Subject));
        }

        return Result.Success();
    }

    public Result Resolve(DateTimeOffset now)
    {
        if (Status is TicketStatus.Resolved)
        {
            return Result.Failure(SupportErrors.TicketAlreadyResolved);
        }

        // BR-SUP-003 read the other way: a ticket nobody handled cannot have been resolved by
        // anybody, and letting it close would lose who to ask when it comes back.
        if (AgentUserId is null)
        {
            return Result.Failure(SupportErrors.AgentRequired);
        }

        Status = TicketStatus.Resolved;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>
    /// BR-SUP-007. Clears the rating, because the question it answered — "did we help" — is open
    /// again. Keeping five stars on a reopened ticket would report satisfaction that was withdrawn.
    /// </summary>
    public Result Reopen(DateTimeOffset now)
    {
        if (Status is not TicketStatus.Resolved)
        {
            return Result.Failure(SupportErrors.TicketNotReopenable);
        }

        Status = TicketStatus.InReview;
        Rating = null;
        Review = null;
        UpdatedAt = now;

        return Result.Success();
    }

    /// <summary>BR-SUP-005 and BR-SUP-006.</summary>
    public Result Rate(int stars, string? review, DateTimeOffset now)
    {
        if (Status is not TicketStatus.Resolved)
        {
            return Result.Failure(SupportErrors.TicketNotResolved);
        }

        if (stars is < 1 or > 5)
        {
            return Result.Failure(SupportErrors.RatingOutOfRange);
        }

        Rating = stars;

        var trimmed = review?.Trim();

        Review = string.IsNullOrWhiteSpace(trimmed)
            ? null
            : trimmed.Length > MaxReviewLength ? trimmed[..MaxReviewLength] : trimmed;

        UpdatedAt = now;

        return Result.Success();
    }
}
