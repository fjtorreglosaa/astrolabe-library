using Astrolabe.Application.Shared.Catalog;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using Astrolabe.Domain.Primitives;
using FluentAssertions;

namespace Astrolabe.Application.Tests.Shared.Catalog;

/// <summary>
/// Covers what the member-facing projection is allowed to show.
///
/// <para>
/// Written for <c>NET-025</c>. BR-NET-005 says a deactivated branch is hidden from members while its
/// history is preserved, and nothing enforced the first half: a withdrawn library kept appearing in
/// the catalogue with its copies reservable. Confirmed against the running system before the fix.
/// </para>
/// </summary>
[TestFixture]
public sealed class BookProjectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid CityId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Midtown = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Harlem = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static Book ABook(int atMidtown = 2, int atHarlem = 3)
    {
        var book = Book.CreateDraft(
            Isbn.Create("9780553383806").Value, "The House of the Spirits", "Isabel Allende",
            null, Genre.Fiction, PlanTier.Basic, Money.FromUnits(18), null, Now).Value;

        book.AddCopies(Midtown, atMidtown);
        book.AddCopies(Harlem, atHarlem);
        book.Publish(Now);
        book.ClearDomainEvents();

        return book;
    }

    private static MemberEntitlement AMaxMember() =>
        PlanCatalog.EntitlementFor(PlanTier.Max, CityId, Midtown);

    private static Dictionary<Guid, BookProjection.LibraryLocation> Libraries(bool harlemIsOpen) =>
        new()
        {
            [Midtown] = new(Midtown, "Midtown", CityId, "New York", IsActive: true),
            [Harlem] = new(Harlem, "Harlem", CityId, "New York", IsActive: harlemIsOpen),
        };

    [Test]
    public void Detail_DropsCopiesHeldAtAWithdrawnBranch()
    {
        var detail = BookProjection.ToDetail(ABook(), AMaxMember(), Libraries(harlemIsOpen: false));

        detail.Copies.Should().ContainSingle().Which.LibraryName.Should().Be("Midtown");
    }

    [Test]
    public void Detail_ListsEveryBranchWhileTheyAreAllOpen()
    {
        var detail = BookProjection.ToDetail(ABook(), AMaxMember(), Libraries(harlemIsOpen: true));

        detail.Copies.Select(copy => copy.LibraryName)
            .Should().BeEquivalentTo(["Midtown", "Harlem"]);
    }

    [Test]
    public void Summary_CountsOnlyStockAMemberCanActuallyReach()
    {
        // The count is what the listing shows. Leaving a withdrawn branch's volumes in it would
        // advertise five copies of which only two can be borrowed.
        var summary = BookProjection.ToSummary(ABook(atMidtown: 2, atHarlem: 3), AMaxMember(),
            Libraries(harlemIsOpen: false));

        summary.AvailableCount.Should().Be(2);
        summary.TotalCount.Should().Be(2);
    }

    [Test]
    public void Summary_CannotBeReservedWhenTheOnlyBranchHoldingItIsWithdrawn()
    {
        var book = ABook(atMidtown: 0, atHarlem: 3);

        var summary = BookProjection.ToSummary(book, AMaxMember(), Libraries(harlemIsOpen: false));

        summary.AvailableCount.Should().Be(0);
        summary.CanReserve.Should().BeFalse();
    }

    [Test]
    public void StaffRow_StillSeesEverything()
    {
        // Staff run the wind-down. Hiding a withdrawn branch's stock from them would hide the work.
        var row = BookProjection.ToStaffRow(ABook(atMidtown: 2, atHarlem: 3));

        row.TotalCount.Should().Be(5);
    }

    [Test]
    public void ACopyAtAnUnknownLibraryIsKept()
    {
        // A missing entry is a data fault, not a withdrawal. Dropping it would quietly shrink a
        // book's holdings and make the fault invisible; it is kept, and an empty city keeps it from
        // ever being judged reachable.
        var detail = BookProjection.ToDetail(
            ABook(), AMaxMember(),
            new Dictionary<Guid, BookProjection.LibraryLocation>
            {
                [Midtown] = new(Midtown, "Midtown", CityId, "New York", IsActive: true),
            });

        detail.Copies.Should().HaveCount(2);
        detail.Copies.Should().Contain(copy => copy.LibraryName == "Unknown library");
    }
}
