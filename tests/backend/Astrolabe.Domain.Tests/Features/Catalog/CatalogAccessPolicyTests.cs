using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.Policies;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Features.Membership.ValueObjects;
using FluentAssertions;

namespace Astrolabe.Domain.Tests.Features.Catalog;

/// <summary>
/// Covers BR-CAT-006 to BR-CAT-016 — the access rule, the most consequential in the product.
///
/// <para>
/// The policy is a pure function, so the entire matrix of AC-CAT-009 runs here in milliseconds:
/// 3 plans × 3 tiers × in and out of city × in and out of the home library × with and without
/// stock. That is the whole reason it was written without a repository.
/// </para>
/// </summary>
[TestFixture]
public sealed class CatalogAccessPolicyTests
{
    private static readonly Guid HomeCity = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid OtherCity = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid HomeLibrary = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid SameCityBranch = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid OtherCityBranch = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static MemberEntitlement Member(PlanTier plan) =>
        PlanCatalog.EntitlementFor(plan, HomeCity, HomeLibrary);

    private static CopyLocation AtHome(int stock = 3) => new(HomeLibrary, HomeCity, stock);

    private static CopyLocation AtSameCityBranch(int stock = 3) => new(SameCityBranch, HomeCity, stock);

    private static CopyLocation AtOtherCity(int stock = 3) => new(OtherCityBranch, OtherCity, stock);

    // ---------- Basic, BR-CAT-007 ----------

    [Test]
    public void Basic_CanReserveABasicBookAtTheHomeLibrary()
    {
        // AC-CAT-001.
        var verdict = CatalogAccessPolicy.EvaluateCopy(Member(PlanTier.Basic), PlanTier.Basic, AtHome());

        verdict.CanReserve.Should().BeTrue();
        verdict.Reason.Should().BeNull();
    }

    [TestCase(PlanTier.Plus)]
    [TestCase(PlanTier.Max)]
    public void Basic_CannotReserveAnyHigherTier_EvenAtTheHomeLibrary(PlanTier bookTier)
    {
        // AC-CAT-002. The tier is checked before the location, so the reason names the plan.
        var verdict = CatalogAccessPolicy.EvaluateCopy(Member(PlanTier.Basic), bookTier, AtHome());

        verdict.CanReserve.Should().BeFalse();
        verdict.Reason.Should().Be(CopyRejection.NotInBasicCatalog);
    }

    [Test]
    public void Basic_CannotReserveABasicBookAtAnotherBranchOfTheirOwnCity()
    {
        // AC-CAT-003. The stock exists, but not where this plan can reach it.
        var verdict = CatalogAccessPolicy.EvaluateCopy(
            Member(PlanTier.Basic), PlanTier.Basic, AtSameCityBranch());

        verdict.CanReserve.Should().BeFalse();
        verdict.Reason.Should().Be(CopyRejection.HomeLibraryOnly);
    }

    // ---------- Plus, BR-CAT-008 ----------

    [TestCase(PlanTier.Basic)]
    [TestCase(PlanTier.Plus)]
    [TestCase(PlanTier.Max)]
    public void Plus_CanReserveAnyTierAnywhereInTheirCity(PlanTier bookTier)
    {
        // AC-CAT-004. A city plan carries no tier restriction at all.
        CatalogAccessPolicy.EvaluateCopy(Member(PlanTier.Plus), bookTier, AtHome())
            .CanReserve.Should().BeTrue();
        CatalogAccessPolicy.EvaluateCopy(Member(PlanTier.Plus), bookTier, AtSameCityBranch())
            .CanReserve.Should().BeTrue();
    }

    [Test]
    public void Plus_CannotReserveACopyInAnotherCity()
    {
        // AC-CAT-005.
        var verdict = CatalogAccessPolicy.EvaluateCopy(
            Member(PlanTier.Plus), PlanTier.Basic, AtOtherCity());

        verdict.CanReserve.Should().BeFalse();
        verdict.Reason.Should().Be(CopyRejection.OutsideCity);
    }

    // ---------- Max, BR-CAT-009 ----------

    [TestCase(PlanTier.Basic)]
    [TestCase(PlanTier.Plus)]
    [TestCase(PlanTier.Max)]
    public void Max_CanReserveAnyTierAnywhereInTheNetwork(PlanTier bookTier)
    {
        // AC-CAT-006.
        foreach (var copy in new[] { AtHome(), AtSameCityBranch(), AtOtherCity() })
        {
            CatalogAccessPolicy.EvaluateCopy(Member(PlanTier.Max), bookTier, copy)
                .CanReserve.Should().BeTrue();
        }
    }

    // ---------- Stock, BR-CAT-006 ----------

    [TestCase(PlanTier.Basic)]
    [TestCase(PlanTier.Plus)]
    [TestCase(PlanTier.Max)]
    public void NoPlanCanReserveACopyWithoutStock(PlanTier plan)
    {
        // AC-CAT-007. Checked before the plan, so an empty shelf never reads as a reason to upgrade.
        var verdict = CatalogAccessPolicy.EvaluateCopy(Member(plan), PlanTier.Basic, AtHome(stock: 0));

        verdict.CanReserve.Should().BeFalse();
        verdict.Reason.Should().Be(CopyRejection.OutOfStock);
    }

    // ---------- The whole book, BR-CAT-010 to BR-CAT-014 ----------

    [Test]
    public void ABookIsReservable_WhenAtLeastOneCopyIs()
    {
        // BR-CAT-010. Two of the three copies are out of reach; one is enough.
        var verdict = CatalogAccessPolicy.EvaluateBook(
            Member(PlanTier.Basic), PlanTier.Basic,
            [AtOtherCity(), AtSameCityBranch(), AtHome()]);

        verdict.CanReserve.Should().BeTrue();
        verdict.Badge.Should().BeNull();
        verdict.Copies.Should().HaveCount(3);
    }

    [Test]
    public void EveryCopyOut_IsBadgedAllCopiesOut()
    {
        // BR-CAT-011.
        var verdict = CatalogAccessPolicy.EvaluateBook(
            Member(PlanTier.Max), PlanTier.Max,
            [AtHome(stock: 0), AtOtherCity(stock: 0)]);

        verdict.CanReserve.Should().BeFalse();
        verdict.Badge.Should().Be(BookRejection.AllCopiesOut);
    }

    [Test]
    public void ATierMismatch_OutranksAnEmptyShelf()
    {
        // BR-CAT-012. Both refusals are true; the member gains nothing from the stock one.
        var verdict = CatalogAccessPolicy.EvaluateBook(
            Member(PlanTier.Basic), PlanTier.Max, [AtHome(stock: 0)]);

        verdict.Badge.Should().Be(BookRejection.NotInBasicPlan);
    }

    [Test]
    public void ATierMismatch_OutranksALocationRefusal()
    {
        var verdict = CatalogAccessPolicy.EvaluateBook(
            Member(PlanTier.Basic), PlanTier.Plus, [AtSameCityBranch()]);

        verdict.Badge.Should().Be(BookRejection.NotInBasicPlan);
    }

    [Test]
    public void StockOutsideABasicMembersBranch_IsBadgedHomeLibraryOnly()
    {
        // BR-CAT-013.
        var verdict = CatalogAccessPolicy.EvaluateBook(
            Member(PlanTier.Basic), PlanTier.Basic, [AtSameCityBranch(), AtOtherCity()]);

        verdict.Badge.Should().Be(BookRejection.HomeLibraryOnly);
    }

    [Test]
    public void StockOutsideAPlusMembersCity_IsBadgedNotInCity()
    {
        // BR-CAT-014.
        var verdict = CatalogAccessPolicy.EvaluateBook(
            Member(PlanTier.Plus), PlanTier.Max, [AtOtherCity()]);

        verdict.Badge.Should().Be(BookRejection.NotInCity);
    }

    [Test]
    public void AMaxMemberIsNeverBadgedForReach()
    {
        // A network plan refuses nothing that has stock, so the fallback badge must stay unreachable.
        var verdict = CatalogAccessPolicy.EvaluateBook(
            Member(PlanTier.Max), PlanTier.Max, [AtOtherCity(), AtSameCityBranch()]);

        verdict.CanReserve.Should().BeTrue();
        verdict.Badge.Should().BeNull();
    }

    [Test]
    public void ABookWithNoCopiesAtAll_IsBadgedAllCopiesOut()
    {
        // A book whose stock rows were all removed must not fall through to a claim about reach.
        var verdict = CatalogAccessPolicy.EvaluateBook(Member(PlanTier.Max), PlanTier.Basic, []);

        verdict.CanReserve.Should().BeFalse();
        verdict.Badge.Should().Be(BookRejection.AllCopiesOut);
        verdict.Copies.Should().BeEmpty();
    }

    // ---------- The full matrix, AC-CAT-009 ----------

    [Test]
    public void TheFullAccessMatrixBehavesAsSpecified()
    {
        var locations = new (string Name, CopyLocation Copy)[]
        {
            ("home library", AtHome()),
            ("same city, other branch", AtSameCityBranch()),
            ("another city", AtOtherCity())
        };

        foreach (var plan in new[] { PlanTier.Basic, PlanTier.Plus, PlanTier.Max })
        {
            foreach (var bookTier in new[] { PlanTier.Basic, PlanTier.Plus, PlanTier.Max })
            {
                foreach (var (name, copy) in locations)
                {
                    var expected = Expected(plan, bookTier, copy);

                    CatalogAccessPolicy.EvaluateCopy(Member(plan), bookTier, copy)
                        .CanReserve.Should().Be(expected,
                            $"a {plan} member looking at a {bookTier} book at the {name}");

                    // Without stock the answer is always no, whatever the plan and the tier.
                    var empty = copy with { AvailableCount = 0 };

                    CatalogAccessPolicy.EvaluateCopy(Member(plan), bookTier, empty)
                        .CanReserve.Should().BeFalse(
                            $"a {plan} member cannot take a {bookTier} book with no stock at the {name}");
                }
            }
        }
    }

    /// <summary>
    /// The rule restated independently of the implementation. Written as three plain conditions
    /// rather than by calling the policy, so a defect in the policy cannot make the matrix agree
    /// with itself.
    /// </summary>
    private static bool Expected(PlanTier plan, PlanTier bookTier, CopyLocation copy) => plan switch
    {
        PlanTier.Basic => bookTier == PlanTier.Basic && copy.LibraryId == HomeLibrary,
        PlanTier.Plus => copy.CityId == HomeCity,
        _ => true
    };
}
