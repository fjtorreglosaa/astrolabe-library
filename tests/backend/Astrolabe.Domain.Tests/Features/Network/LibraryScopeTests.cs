using Astrolabe.Domain.Features.Network.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Network;

/// <summary>
/// Covers BR-NET-006, BR-NET-007 and BR-NET-010. This is the value every other domain consults to
/// decide whether a staff user may act, so it carries more weight than its size suggests.
/// </summary>
[TestFixture]
public sealed class LibraryScopeTests
{
    private static readonly Guid Midtown = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Harlem = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid Loop = Guid.Parse("33333333-3333-3333-3333-333333333333");

    [Test]
    public void Unrestricted_CoversEveryLibrary()
    {
        // BR-NET-007: a super administrator never requires an assignment.
        var scope = LibraryScope.Unrestricted();

        scope.Covers(Midtown).Should().BeTrue();
        scope.Covers(Guid.NewGuid()).Should().BeTrue();
        scope.IsUnrestricted.Should().BeTrue();
        scope.IsEmpty.Should().BeFalse();
    }

    [Test]
    public void Of_CoversOnlyTheAssignedLibraries()
    {
        // BR-NET-006, and AC-NET-001: Midtown and Harlem yes, Chicago no.
        var scope = LibraryScope.Of([Midtown, Harlem]);

        scope.Covers(Midtown).Should().BeTrue();
        scope.Covers(Harlem).Should().BeTrue();
        scope.Covers(Loop).Should().BeFalse();
    }

    [Test]
    public void Empty_CoversNothingButIsAValidState()
    {
        // BR-NET-010: an administrator with no assignments sees empty lists, not an error.
        var scope = LibraryScope.Empty();

        scope.IsEmpty.Should().BeTrue();
        scope.IsUnrestricted.Should().BeFalse();
        scope.Covers(Midtown).Should().BeFalse();
        scope.Filter([Midtown, Harlem]).Should().BeEmpty();
    }

    [Test]
    public void CoversAll_RequiresEveryLibrary()
    {
        var scope = LibraryScope.Of([Midtown, Harlem]);

        scope.CoversAll([Midtown, Harlem]).Should().BeTrue();
        scope.CoversAll([Midtown, Loop]).Should().BeFalse();
    }

    [Test]
    public void CoversAll_WithNoLibraries_IsVacuouslyTrue()
    {
        // An operation touching no library needs no library authority.
        LibraryScope.Empty().CoversAll([]).Should().BeTrue();
    }

    [Test]
    public void CoversAny_RequiresOnlyOne()
    {
        var scope = LibraryScope.Of([Midtown]);

        scope.CoversAny([Midtown, Loop]).Should().BeTrue();
        scope.CoversAny([Loop]).Should().BeFalse();
        scope.CoversAny([]).Should().BeFalse();
    }

    [Test]
    public void Filter_NarrowsToWhatIsAllowed()
    {
        // List queries narrow rather than reject, so asking too broadly is not an error.
        var scope = LibraryScope.Of([Midtown, Harlem]);

        scope.Filter([Midtown, Harlem, Loop]).Should().BeEquivalentTo([Midtown, Harlem]);
    }

    [Test]
    public void Filter_OnUnrestricted_KeepsEverything()
    {
        LibraryScope.Unrestricted()
            .Filter([Midtown, Loop])
            .Should().BeEquivalentTo([Midtown, Loop]);
    }

    [Test]
    public void Unrestricted_IsNeverEmpty_EvenWithNoLibraryIds()
    {
        // The distinction that matters: a super administrator with no assignments still sees
        // everything, while an administrator with no assignments sees nothing.
        LibraryScope.Unrestricted().IsEmpty.Should().BeFalse();
        LibraryScope.Empty().IsEmpty.Should().BeTrue();
    }

    [Test]
    public void Equality_IsByValue_IgnoringOrder()
    {
        LibraryScope.Of([Midtown, Harlem])
            .Should().Be(LibraryScope.Of([Harlem, Midtown]));
    }

    [Test]
    public void Equality_DistinguishesUnrestrictedFromEmpty()
    {
        LibraryScope.Unrestricted().Should().NotBe(LibraryScope.Empty());
    }

    [Test]
    public void Of_WithDuplicates_DeduplicatesSilently()
    {
        LibraryScope.Of([Midtown, Midtown]).LibraryIds.Should().HaveCount(1);
    }
}
