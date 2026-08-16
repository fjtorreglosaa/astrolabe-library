using Astrolabe.Application.Contracts.Network;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Reservations.Entities;
using Astrolabe.Domain.Features.Reservations.Enums;
using Astrolabe.Infrastructure.Features.Network;
using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Infrastructure.Tests.TestSupport;
using FluentAssertions;

namespace Astrolabe.Infrastructure.Tests.Features.Network;

/// <summary>
/// Covers the probe that replaced the placeholder answering "none" to everything (<c>NET-025</c>).
///
/// <para>
/// It reports rather than refuses — see <c>LibraryObligations</c> — so what matters here is that the
/// numbers are true. A report nobody can trust is worse than no report: it would send an operator to
/// wind down a branch that is already empty, or leave one with live loans looking finished.
/// </para>
/// </summary>
[TestFixture]
public sealed class LibraryObligationsProbeTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Midtown = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Harlem = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid MemberId = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private AstrolabeDbContext _context = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp() => _context = TestDbContext.Create();

    [TearDown]
    public void TearDown() => _context.Dispose();

    private LibraryObligationsProbe Probe() => new(_context);

    private void AddCopies(Guid libraryId, int quantity)
    {
        _context.BookCopies.Add(BookCopy.Create(Guid.NewGuid(), libraryId, quantity).Value);
        _context.SaveChanges();
    }

    private Reservation AddReservation(Guid libraryId)
    {
        var reservation = Reservation.Confirm(
            MemberId, Guid.NewGuid(), Guid.NewGuid(), libraryId,
            DeliveryMethod.Collection, null, Now);

        reservation.ClearDomainEvents();
        _context.Reservations.Add(reservation);
        _context.SaveChanges();

        return reservation;
    }

    /// <summary>Starts the return, which is what moves a reservation to InTransit.</summary>
    private void Handover(Reservation reservation)
    {
        reservation.BeginReturn(
            MemberId, ReturnMethod.CourierPickup, reservation.HandoverCode.Value, Now)
            .IsSuccess.Should().BeTrue();

        reservation.ClearDomainEvents();
        _context.SaveChanges();
    }

    private Fine AddFine(Guid libraryId, int daysLate = 3)
    {
        var fine = Fine.Assess(MemberId, Guid.NewGuid(), libraryId, "Any title", daysLate, Now)!;

        _context.Fines.Add(fine);
        _context.SaveChanges();

        return fine;
    }

    [Test]
    public async Task AnUntouchedLibraryOwesNothing()
    {
        var report = await Probe().GetAsync(Midtown, Ct);

        report.HasAny.Should().BeFalse();
        report.Should().Be(LibraryObligations.None);
    }

    [Test]
    public async Task CopiesAreCountedAsVolumes_NotAsRows()
    {
        // One row records how many copies of one title a branch holds. Counting rows would report
        // "two books" where the shelves carry seven, and an operator would plan the wrong wind-down.
        AddCopies(Midtown, 4);
        AddCopies(Midtown, 3);

        (await Probe().GetAsync(Midtown, Ct)).Copies.Should().Be(7);
    }

    [Test]
    public async Task OnlyLiveReservationsCount()
    {
        AddReservation(Midtown);                                       // Reserved — live

        var inTransit = AddReservation(Midtown);                       // InTransit — still live:
        Handover(inTransit);                                           // the copy is not back yet

        var returned = AddReservation(Midtown);                        // Returned — settled
        Handover(returned);
        returned.CheckIn(Now);
        _context.SaveChanges();

        (await Probe().GetAsync(Midtown, Ct)).ActiveReservations.Should().Be(2);
    }

    [Test]
    public async Task AFineAwaitingValidationIsStillUnresolved()
    {
        // The member has paid at a desk but no administrator has confirmed it, so the money is not
        // settled. Treating it as resolved would report a branch as clear while cash is unaccounted.
        var fine = AddFine(Midtown);
        fine.Hold(Guid.NewGuid());
        _context.SaveChanges();

        (await Probe().GetAsync(Midtown, Ct)).UnresolvedFines.Should().Be(1);
    }

    [Test]
    public async Task ASettledFineIsNotCounted()
    {
        var fine = AddFine(Midtown);
        fine.Hold(Guid.NewGuid());
        fine.Settle(Now);
        _context.SaveChanges();

        (await Probe().GetAsync(Midtown, Ct)).UnresolvedFines.Should().Be(0);
    }

    [Test]
    public async Task EachBranchIsReportedSeparately()
    {
        // The whole point of the report is per-branch. A missing filter would make every library
        // look busy and no withdrawal would ever read as safe.
        AddCopies(Midtown, 5);
        AddReservation(Midtown);
        AddFine(Harlem);

        var midtown = await Probe().GetAsync(Midtown, Ct);
        var harlem = await Probe().GetAsync(Harlem, Ct);

        midtown.Should().Be(new LibraryObligations(5, 1, 0));
        harlem.Should().Be(new LibraryObligations(0, 0, 1));
    }
}
