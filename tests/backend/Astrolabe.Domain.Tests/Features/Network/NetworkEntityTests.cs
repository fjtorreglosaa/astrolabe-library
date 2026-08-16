using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Events;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Network;

/// <summary>
/// Covers the network entities and the edge cases recorded in network.business.md section 5.
/// </summary>
[TestFixture]
public sealed class NetworkEntityTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 15, 12, 0, 0, TimeSpan.Zero);

    private static City ACity(Guid? id = null) =>
        City.Create(id ?? Guid.NewGuid(), Guid.NewGuid(), "New York").Value;

    private static Library ALibrary(Guid cityId, string name = "Midtown") =>
        Library.Create(Guid.NewGuid(), cityId, name).Value;

    // ---------- Country ----------

    [Test]
    public void Country_Create_NormalisesTheIsoCode()
    {
        var country = Country.Create(Guid.NewGuid(), "  United States  ", "us").Value;

        country.Name.Should().Be("United States");
        country.IsoCode.Should().Be("US");
    }

    [TestCase("")]
    [TestCase("   ")]
    public void Country_Create_WithoutAName_Fails(string name)
    {
        Country.Create(Guid.NewGuid(), name, "US").IsFailure.Should().BeTrue();
    }

    [TestCase("USA")]
    [TestCase("U")]
    [TestCase("")]
    public void Country_Create_WithAnInvalidIsoCode_Fails(string iso)
    {
        Country.Create(Guid.NewGuid(), "United States", iso).IsFailure.Should().BeTrue();
    }

    [Test]
    public void Country_IsShownInRegistrationByDefault()
    {
        Country.Create(Guid.NewGuid(), "Spain", "ES").Value
            .IsHiddenFromRegistration.Should().BeFalse();
    }

    // ---------- City and home library, BR-NET-003 ----------

    [Test]
    public void City_DesignateHomeLibrary_AcceptsItsOwnActiveLibrary()
    {
        var city = ACity();
        var library = ALibrary(city.Id);

        city.DesignateHomeLibrary(library).IsSuccess.Should().BeTrue();
        city.HomeLibraryId.Should().Be(library.Id);
        city.IsHomeLibrary(library.Id).Should().BeTrue();
    }

    [Test]
    public void City_DesignateHomeLibrary_RejectsALibraryFromAnotherCity()
    {
        var city = ACity();
        var foreign = ALibrary(Guid.NewGuid());

        var result = city.DesignateHomeLibrary(foreign);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(NetworkErrors.HomeLibraryNotInCity);
        city.HomeLibraryId.Should().BeNull();
    }

    [Test]
    public void City_DesignateHomeLibrary_RejectsAnInactiveLibrary()
    {
        var city = ACity();
        var library = ALibrary(city.Id);
        library.Deactivate(isCityHomeLibrary: false, hasOpenObligations: false);

        var result = city.DesignateHomeLibrary(library);

        result.Error.Should().Be(NetworkErrors.HomeLibraryInactive);
    }

    // ---------- Library deactivation, BR-NET-005 ----------

    [Test]
    public void Library_IsActiveWhenCreated()
    {
        ALibrary(Guid.NewGuid()).IsActive.Should().BeTrue();
    }

    [Test]
    public void Library_Deactivate_WithNothingOutstanding_Succeeds()
    {
        var library = ALibrary(Guid.NewGuid());

        library.Deactivate(isCityHomeLibrary: false, hasOpenObligations: false)
            .IsSuccess.Should().BeTrue();
        library.IsActive.Should().BeFalse();
    }

    [Test]
    public void Library_Deactivate_TheCityHomeLibrary_IsBlocked()
    {
        // Edge case: a city must always expose a home library, so another must be designated first.
        var library = ALibrary(Guid.NewGuid());

        var result = library.Deactivate(isCityHomeLibrary: true, hasOpenObligations: false);

        result.Error.Should().Be(NetworkErrors.CannotDeactivateHomeLibrary);
        library.IsActive.Should().BeTrue();
    }

    [Test]
    public void Library_Deactivate_WithOpenObligations_IsBlocked()
    {
        var library = ALibrary(Guid.NewGuid());

        var result = library.Deactivate(isCityHomeLibrary: false, hasOpenObligations: true);

        result.Error.Should().Be(NetworkErrors.LibraryHasOpenObligations);
        library.IsActive.Should().BeTrue();
    }

    [Test]
    public void Library_Deactivate_Twice_Fails()
    {
        var library = ALibrary(Guid.NewGuid());
        library.Deactivate(false, false);

        library.Deactivate(false, false).Error.Should().Be(NetworkErrors.LibraryAlreadyInactive);
    }

    [Test]
    public void Library_HomeLibraryCheckTakesPrecedenceOverObligations()
    {
        // Both blockers present: the caller must be told to designate another home library, which is
        // the action that actually unblocks them.
        var library = ALibrary(Guid.NewGuid());

        library.Deactivate(isCityHomeLibrary: true, hasOpenObligations: true)
            .Error.Should().Be(NetworkErrors.CannotDeactivateHomeLibrary);
    }

    // ---------- Assignments, BR-NET-009 and BR-NET-011 ----------

    [Test]
    public void Assignment_Grant_IsActiveAndRaisesAnEvent()
    {
        var assignment = LibraryAssignment.Grant(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);

        assignment.IsActive.Should().BeTrue();
        assignment.RevokedAt.Should().BeNull();
        assignment.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<LibraryAssigned>();
    }

    [Test]
    public void Assignment_Revoke_MarksItRatherThanDeletingIt()
    {
        // BR-NET-017 needs something to audit against, so revocation preserves the row.
        var revoker = Guid.NewGuid();
        var assignment = LibraryAssignment.Grant(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);

        assignment.Revoke(revoker, Now.AddDays(1)).IsSuccess.Should().BeTrue();

        assignment.IsActive.Should().BeFalse();
        assignment.RevokedAt.Should().Be(Now.AddDays(1));
        assignment.RevokedByUserId.Should().Be(revoker);
        assignment.DomainEvents.Should().HaveCount(2);
        assignment.DomainEvents[1].Should().BeOfType<LibraryAssignmentRevoked>();
    }

    [Test]
    public void Assignment_RevokeTwice_Fails()
    {
        var assignment = LibraryAssignment.Grant(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Now);
        assignment.Revoke(Guid.NewGuid(), Now);

        assignment.Revoke(Guid.NewGuid(), Now).Error
            .Should().Be(NetworkErrors.AssignmentAlreadyRevoked);
    }

    // ---------- Invitations, BR-NET-013 to BR-NET-015 ----------

    private static AdminInvitation AnInvitation(
        UserRole role = UserRole.Admin, IReadOnlyList<Guid>? libraries = null) =>
        AdminInvitation.Create(
            Guid.NewGuid(), Guid.NewGuid(), role,
            libraries ?? [Guid.NewGuid()],
            [1, 2, 3], Guid.NewGuid(), Now, TimeSpan.FromDays(7)).Value;

    [Test]
    public void Invitation_Create_CarriesItsOwnRoleAndLibraries()
    {
        // This is what lets an invitation survive its sender being revoked.
        var libraries = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var invitation = AnInvitation(UserRole.Admin, libraries);

        invitation.Role.Should().Be(UserRole.Admin);
        invitation.LibraryIds.Should().BeEquivalentTo(libraries);
        invitation.IsPending.Should().BeTrue();
        invitation.DomainEvents.Should().ContainSingle().Which.Should().BeOfType<AdminInvited>();
    }

    [Test]
    public void Invitation_Create_WithAMemberRole_Fails()
    {
        AdminInvitation.Create(
            Guid.NewGuid(), Guid.NewGuid(), UserRole.Member, [Guid.NewGuid()],
            [1], Guid.NewGuid(), Now, TimeSpan.FromDays(7))
            .Error.Should().Be(NetworkErrors.InvitationRoleInvalid);
    }

    [Test]
    public void Invitation_Create_ForAnAdminWithoutLibraries_Fails()
    {
        // An Admin with no libraries could never act, so the invitation is meaningless.
        AdminInvitation.Create(
            Guid.NewGuid(), Guid.NewGuid(), UserRole.Admin, [],
            [1], Guid.NewGuid(), Now, TimeSpan.FromDays(7))
            .Error.Should().Be(NetworkErrors.InvitationLibrariesRequired);
    }

    [Test]
    public void Invitation_Create_ForASuperAdminWithoutLibraries_Succeeds()
    {
        // BR-NET-007: a super administrator has unrestricted scope, so naming libraries is meaningless.
        AdminInvitation.Create(
            Guid.NewGuid(), Guid.NewGuid(), UserRole.SuperAdmin, [],
            [1], Guid.NewGuid(), Now, TimeSpan.FromDays(7))
            .IsSuccess.Should().BeTrue();
    }

    [Test]
    public void Invitation_Accept_WithinItsLifetime_Succeeds()
    {
        var invitation = AnInvitation();

        invitation.Accept(Now.AddDays(1)).IsSuccess.Should().BeTrue();
        invitation.AcceptedAt.Should().Be(Now.AddDays(1));
        invitation.IsPending.Should().BeFalse();
    }

    [Test]
    public void Invitation_Accept_Twice_Fails()
    {
        var invitation = AnInvitation();
        invitation.Accept(Now.AddDays(1));

        invitation.Accept(Now.AddDays(2)).Error
            .Should().Be(NetworkErrors.InvitationAlreadyAccepted);
    }

    [Test]
    public void Invitation_Accept_AfterExpiry_Fails()
    {
        var invitation = AnInvitation();

        invitation.Accept(Now.AddDays(8)).Error.Should().Be(NetworkErrors.InvitationExpired);
    }

    [Test]
    public void Invitation_Accept_AfterRevocation_Fails()
    {
        // BR-NET-015: resending revokes the previous invitation, so its link must stop working.
        var invitation = AnInvitation();
        invitation.Revoke(Now.AddHours(1));

        invitation.Accept(Now.AddHours(2)).Error.Should().Be(NetworkErrors.InvitationRevoked);
    }

    [Test]
    public void Invitation_Revoke_AfterAcceptance_DoesNothing()
    {
        // An accepted invitation is spent. Revoking it must not retroactively invalidate the account.
        var invitation = AnInvitation();
        invitation.Accept(Now.AddDays(1));

        invitation.Revoke(Now.AddDays(2));

        invitation.RevokedAt.Should().BeNull();
        invitation.AcceptedAt.Should().NotBeNull();
    }
}
