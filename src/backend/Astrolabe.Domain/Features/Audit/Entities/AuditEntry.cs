using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Audit.Entities;

/// <summary>
/// An append-only record of a security-relevant event. Implements BR-IDN-032 and BR-IDN-033.
///
/// It has no mutating members at all: an audit entry that can be edited is not an audit trail.
/// <see cref="Detail"/> is free text for context and must never carry a password, a token, or a
/// token hash.
/// </summary>
public sealed class AuditEntry : Entity
{
    private AuditEntry()
    {
    }

    private AuditEntry(
        Guid id, string action, Guid? actorUserId, Guid? subjectUserId,
        string? ipAddress, string? detail, DateTimeOffset occurredAt) : base(id)
    {
        Action = action;
        ActorUserId = actorUserId;
        SubjectUserId = subjectUserId;
        IpAddress = ipAddress;
        Detail = detail;
        OccurredAt = occurredAt;
    }

    /// <summary>What happened, for example <c>identity.sign_in_failed</c>.</summary>
    public string Action { get; private set; } = string.Empty;

    /// <summary>Who acted. Null for an anonymous attempt, such as a failed sign-in.</summary>
    public Guid? ActorUserId { get; private set; }

    /// <summary>Who it happened to. Equal to the actor for self-service operations.</summary>
    public Guid? SubjectUserId { get; private set; }

    public string? IpAddress { get; private set; }

    public string? Detail { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static AuditEntry Record(
        string action,
        DateTimeOffset occurredAt,
        Guid? actorUserId = null,
        Guid? subjectUserId = null,
        string? ipAddress = null,
        string? detail = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return new AuditEntry(
            Guid.NewGuid(), action, actorUserId, subjectUserId, ipAddress, detail, occurredAt);
    }
}
