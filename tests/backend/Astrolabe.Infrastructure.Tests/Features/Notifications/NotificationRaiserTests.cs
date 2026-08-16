using Astrolabe.Domain.Features.Notifications.Entities;
using Astrolabe.Domain.Features.Notifications.Enums;
using Astrolabe.Domain.Features.Notifications.Repositories;
using Astrolabe.Infrastructure.Features.Notifications;
using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Infrastructure.Persistence.Repositories.Notifications;
using Astrolabe.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Astrolabe.Infrastructure.Tests.Features.Notifications;

/// <summary>
/// Covers the one place a notification is created. BR-NTF-003 and BR-NTF-005.
///
/// <para>
/// The last test is the one that matters most. Every caller of this seam is a domain event handler
/// reacting to something already committed, so a failure here must never propagate — a message about
/// a reservation must not be able to cost somebody the reservation.
/// </para>
/// </summary>
[TestFixture]
public sealed class NotificationRaiserTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid MemberId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private AstrolabeDbContext _context = null!;
    private INotificationsUnitOfWork _unitOfWork = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContext.Create();
        _unitOfWork = new NotificationsUnitOfWork(
            _context,
            new NotificationRepository(_context),
            new NotificationPreferenceRepository(_context));
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private NotificationRaiser Raiser() =>
        new(_unitOfWork, new FixedClock(Now), NullLogger<NotificationRaiser>.Instance);

    private async Task MuteAsync(NotificationFamily family)
    {
        _context.NotificationPreferences.Add(
            NotificationPreference.Mute(MemberId, family, Now));

        await _context.SaveChangesAsync(Ct);
    }

    private Task<int> CountAsync() =>
        _context.Notifications.CountAsync(n => n.MemberId == MemberId, Ct);

    [Test]
    public async Task AnUnmutedKindIsDelivered()
    {
        await Raiser().RaiseAsync(MemberId, NotificationKind.Due, "Overdue", "Detail", "/loans", Ct);

        (await CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task AMutedFamilyProducesNothingAtAll()
    {
        // BR-NTF-003, and the wording is deliberate: nothing is written. A notification created and
        // hidden is one that reappears the day somebody writes a query that forgets the filter.
        await MuteAsync(NotificationFamily.Payments);

        await Raiser().RaiseAsync(MemberId, NotificationKind.Paid, "Paid", "Detail", "/fines", Ct);

        (await CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task MutingOneFamilyLeavesTheOthersAlone()
    {
        await MuteAsync(NotificationFamily.Payments);

        await Raiser().RaiseAsync(MemberId, NotificationKind.Desk, "Desk", "D", null, Ct);
        await Raiser().RaiseAsync(MemberId, NotificationKind.Due, "Due", "D", null, Ct);

        (await CountAsync()).Should().Be(1, "only the payments family was muted");
    }

    [Test]
    public async Task EveryKindInAMutedFamilyIsSilenced()
    {
        // The point of families. A member who mutes payments does not expect the desk one to survive
        // because it has a different icon.
        await MuteAsync(NotificationFamily.Payments);

        foreach (var kind in new[]
                 { NotificationKind.Paid, NotificationKind.Pending, NotificationKind.Desk })
        {
            await Raiser().RaiseAsync(MemberId, kind, "Title", "Body", null, Ct);
        }

        (await CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task MutingDoesNotTouchWhatWasAlreadyDelivered()
    {
        // BR-NTF-004. Silencing a family is a decision about the future; taking back what somebody
        // was already told would be rewriting their history.
        await Raiser().RaiseAsync(MemberId, NotificationKind.Paid, "Paid", "Detail", null, Ct);
        await MuteAsync(NotificationFamily.Payments);

        (await CountAsync()).Should().Be(1);
    }

    [Test]
    public async Task ARefusedNotificationDoesNotThrow()
    {
        // An empty title is refused by the aggregate. The raiser logs it and carries on, because the
        // event that caused it has already committed.
        var act = async () =>
            await Raiser().RaiseAsync(MemberId, NotificationKind.Due, "   ", "Body", null, Ct);

        await act.Should().NotThrowAsync();
        (await CountAsync()).Should().Be(0);
    }

    [Test]
    public async Task AFailingStoreNeverPropagates()
    {
        // BR-NTF-005, and the reason the whole seam swallows. The reservation, payment or ticket
        // that triggered this already committed — throwing here would either roll back something
        // that succeeded or leave the caller believing it had not.
        var broken = new Mock<INotificationsUnitOfWork>();
        broken.SetupGet(u => u.Preferences)
            .Returns(new NotificationPreferenceRepository(_context));
        broken.SetupGet(u => u.Notifications)
            .Throws(new InvalidOperationException("the database is on fire"));

        var raiser = new NotificationRaiser(
            broken.Object, new FixedClock(Now), NullLogger<NotificationRaiser>.Instance);

        var act = async () =>
            await raiser.RaiseAsync(MemberId, NotificationKind.Due, "Title", "Body", null, Ct);

        await act.Should().NotThrowAsync();
    }
}
