using Astrolabe.Domain.Abstractions;

namespace Astrolabe.Domain.Features.Store.Entities;

/// <summary>
/// One movement of reward points. Implements BR-STR-018.
///
/// <para>
/// Points are value, so they get the same treatment as money: a balance is the sum of movements,
/// never a stored number. There is no method here that changes a movement after construction.
/// </para>
/// <para>
/// <see cref="PointCents"/> is signed: positive earns, negative redeems. Redemption arrived in
/// <c>STR-017</c> as a second factory and no schema change, which is what the sign was for.
/// </para>
/// </summary>
public sealed class PointsMovement : Entity
{
    private PointsMovement()
    {
    }

    private PointsMovement(
        Guid id, Guid memberId, int pointCents, string description,
        Guid? orderId, DateTimeOffset now) : base(id)
    {
        MemberId = memberId;
        PointCents = pointCents;
        Description = description;
        OrderId = orderId;
        OccurredAt = now;
    }

    public Guid MemberId { get; private set; }

    /// <summary>Signed. Positive is earned, negative is redeemed.</summary>
    public int PointCents { get; private set; }

    public string Description { get; private set; } = string.Empty;

    public Guid? OrderId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static PointsMovement Earned(
        Guid memberId, int pointCents, string description, Guid orderId, DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, Math.Abs(pointCents), description, orderId, now);

    /// <summary>
    /// Points spent on an order. BR-STR-007.
    ///
    /// Stored negative, so the balance stays a plain sum over the movements and no reader has to
    /// know which kinds subtract.
    /// </summary>
    public static PointsMovement Redeemed(
        Guid memberId, int pointCents, string description, Guid orderId, DateTimeOffset now) =>
        new(Guid.NewGuid(), memberId, -Math.Abs(pointCents), description, orderId, now);
}
