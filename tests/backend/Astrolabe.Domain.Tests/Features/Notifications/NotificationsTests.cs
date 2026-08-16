using Astrolabe.Domain.Features.Notifications.Entities;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Notifications.Errors;
using Astrolabe.Domain.Features.Notifications.Policies;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Notifications;

/// <summary>
/// Covers the notifications domain: BR-NTF-001, BR-NTF-002, BR-NTF-006 and BR-NTF-009.
/// </summary>
[TestFixture]
public sealed class NotificationsTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static Notification ANotification(NotificationKind kind = NotificationKind.Due) =>
        Notification.Raise(MemberId, kind, "Something happened", "Some detail.", "/loans", Now).Value;

    // ---------- BR-NTF-002: every kind belongs to exactly one family ----------

    [Test]
    public void EveryKindIsMappedToAFamily()
    {
        // The test that keeps the map honest. A kind added to the enum without a family would
        // otherwise be muted by whichever switch a default named — silently, and by the wrong one.
        var kinds = Enum.GetValues<NotificationKind>();

        NotificationFamilies.All.Should().HaveCount(kinds.Length);

        foreach (var kind in kinds)
        {
            var act = () => NotificationFamilies.Of(kind);
            act.Should().NotThrow($"{kind} has no family");
        }
    }

    [Test]
    public void PaymentKindsShareOneFamily()
    {
        // Somebody who turns off payments means all of them, which is why the family is coarser than
        // the kind and why the settings screen shows five switches rather than eight.
        NotificationFamilies.Of(NotificationKind.Paid).Should().Be(NotificationFamily.Payments);
        NotificationFamilies.Of(NotificationKind.Pending).Should().Be(NotificationFamily.Payments);
        NotificationFamilies.Of(NotificationKind.Desk).Should().Be(NotificationFamily.Payments);
    }

    [Test]
    public void ReturnKindsShareOneFamily()
    {
        NotificationFamilies.Of(NotificationKind.Transit).Should().Be(NotificationFamily.Returns);
        NotificationFamilies.Of(NotificationKind.Returned).Should().Be(NotificationFamily.Returns);
    }

    // ---------- BR-NTF-006: marking read is idempotent ----------

    [Test]
    public void MarkingReadTwiceKeepsTheFirstTime()
    {
        // "When did I read this" has one answer. A second click is not a second reading, and moving
        // the timestamp would quietly make it one.
        var notification = ANotification();

        notification.MarkRead(Now);
        notification.MarkRead(Now.AddHours(3));

        notification.ReadAt.Should().Be(Now);
        notification.IsRead.Should().BeTrue();
    }

    [Test]
    public void ANewNotificationIsUnread()
    {
        ANotification().IsRead.Should().BeFalse();
    }

    // ---------- BR-NTF-009: a notification goes somewhere ----------

    [Test]
    public void ANotificationKeepsItsRoute()
    {
        ANotification().Route.Should().Be("/loans");
    }

    [Test]
    public void ABlankRouteBecomesNull()
    {
        // So a client can test one thing rather than two. An empty string that renders as a link to
        // nowhere is worse than no link.
        Notification.Raise(MemberId, NotificationKind.Due, "Title", "Body", "   ", Now)
            .Value.Route.Should().BeNull();
    }

    // ---------- Content ----------

    [TestCase("")]
    [TestCase("   ")]
    public void ANotificationWithoutATitleIsRefused(string title)
    {
        Notification.Raise(MemberId, NotificationKind.Due, title, "Body", null, Now)
            .Error.Should().Be(NotificationErrors.TitleRequired);
    }

    [Test]
    public void OverlongContentIsTruncatedRatherThanRefused()
    {
        // A message about a real event is worth showing shortened. Refusing it would lose the news
        // entirely because somebody's book title was long.
        var notification = Notification.Raise(
            MemberId, NotificationKind.Due,
            new string('t', Notification.MaxTitleLength + 50),
            new string('b', Notification.MaxBodyLength + 50),
            null, Now).Value;

        notification.Title.Should().HaveLength(Notification.MaxTitleLength);
        notification.Body.Should().HaveLength(Notification.MaxBodyLength);
    }

    [Test]
    public void AMissingBodyIsAllowed()
    {
        // Some events say everything in their title. An empty body is a shape, not a fault.
        Notification.Raise(MemberId, NotificationKind.Hold, "Your hold is ready", null!, null, Now)
            .IsSuccess.Should().BeTrue();
    }

    // ---------- Preferences ----------

    [Test]
    public void MutingRecordsTheFamilyAndWhen()
    {
        var preference = NotificationPreference.Mute(MemberId, NotificationFamily.Payments, Now);

        preference.Family.Should().Be(NotificationFamily.Payments);
        preference.MemberId.Should().Be(MemberId);
        preference.MutedAt.Should().Be(Now);
    }
}
