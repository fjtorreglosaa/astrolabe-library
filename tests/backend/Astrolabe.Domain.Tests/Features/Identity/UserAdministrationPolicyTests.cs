using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.Errors;
using Astrolabe.Domain.Features.Identity.Policies;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Identity;

/// <summary>
/// Covers who may administer whom from the staff user directory.
///
/// <para>
/// Transcribed from the prototype's <c>canManage</c>, which is the authority here and states it
/// exactly: <c>!isSelf &amp;&amp; (isSuper ? !targetSuper : !targetStaff)</c>. Every refusal is
/// asserted with its own error, because the prototype is equally explicit that a control which
/// cannot be used must say why.
/// </para>
/// </summary>
[TestFixture]
public sealed class UserAdministrationPolicyTests
{
    private static readonly Guid Actor = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Target = Guid.Parse("22222222-2222-2222-2222-222222222222");

    // ---------- What a super administrator may do ----------

    [TestCase(UserRole.Member)]
    [TestCase(UserRole.Admin)]
    public void ASuperAdmin_MayAdministerAnyoneBelowThem(UserRole targetRole)
    {
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, UserRole.SuperAdmin, Target, targetRole)
            .IsSuccess.Should().BeTrue();
    }

    [Test]
    public void ASuperAdmin_MayNotAdministerAnotherSuperAdmin()
    {
        // BR-NET-012 in the directory. Two super administrators able to lock each other out is a
        // network that can be left without one.
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, UserRole.SuperAdmin, Target, UserRole.SuperAdmin)
            .Error.Should().Be(IdentityErrors.CannotAdministerASuperAdmin);
    }

    // ---------- What an administrator may do ----------

    [Test]
    public void AnAdmin_MayAdministerAMember()
    {
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, UserRole.Admin, Target, UserRole.Member)
            .IsSuccess.Should().BeTrue();
    }

    [TestCase(UserRole.Admin)]
    [TestCase(UserRole.SuperAdmin)]
    public void AnAdmin_MayNotAdministerStaff(UserRole targetRole)
    {
        // BR-NET-008 reserves creating and revoking administrators to a super administrator. Without
        // this the directory would be a side door into exactly that.
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, UserRole.Admin, Target, targetRole)
            .IsFailure.Should().BeTrue();
    }

    [Test]
    public void AnAdminRefusedAnotherAdmin_IsToldItNeedsASuperAdmin()
    {
        // The reason matters as much as the refusal: this one tells the administrator who to ask.
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, UserRole.Admin, Target, UserRole.Admin)
            .Error.Should().Be(IdentityErrors.SuperAdminRequiredForStaff);
    }

    // ---------- Nobody administers themselves ----------

    [TestCase(UserRole.Admin)]
    [TestCase(UserRole.SuperAdmin)]
    public void NobodyMayAdministerTheirOwnAccount(UserRole actorRole)
    {
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, actorRole, Actor, actorRole)
            .Error.Should().Be(IdentityErrors.CannotAdministerYourself);
    }

    [Test]
    public void TheSelfCheckComesBeforeEveryOther()
    {
        // A super administrator looking at their own row is refused for being themselves, not for
        // being a super administrator. The distinction is the whole message: one is a rule about
        // the console, the other is advice to go and ask a colleague.
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, UserRole.SuperAdmin, Actor, UserRole.SuperAdmin)
            .Error.Should().Be(IdentityErrors.CannotAdministerYourself);
    }

    // ---------- Members are not staff ----------

    [Test]
    public void AMember_MayNotAdministerAnybody()
    {
        // The endpoint policy already refuses them, and this refuses them again. A member who
        // reached this code has got past the outer door, which is precisely when the inner one
        // matters.
        UserAdministrationPolicy
            .EnsureCanAdminister(Actor, UserRole.Member, Target, UserRole.Member)
            .Error.Should().Be(IdentityErrors.StaffRequired);
    }

    [Test]
    public void TheBooleanFormAgreesWithTheResultForm()
    {
        // Two callers ask this question — the projection that draws the button and the handler that
        // enforces it. If they ever disagreed, the screen would offer an action the API refuses.
        UserRole[] roles = [UserRole.Member, UserRole.Admin, UserRole.SuperAdmin];

        foreach (var actorRole in roles)
        {
            foreach (var targetRole in roles)
            {
                UserAdministrationPolicy.CanAdminister(Actor, actorRole, Target, targetRole)
                    .Should().Be(
                        UserAdministrationPolicy
                            .EnsureCanAdminister(Actor, actorRole, Target, targetRole).IsSuccess,
                        $"actor {actorRole} on target {targetRole}");
            }
        }
    }
}
