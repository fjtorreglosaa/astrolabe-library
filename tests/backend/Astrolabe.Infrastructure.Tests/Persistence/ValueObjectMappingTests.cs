using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Primitives;
using Astrolabe.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Astrolabe.Infrastructure.Tests.Persistence;

/// <summary>
/// Regression guard for the defect that shipped in Stage 2.
///
/// <para>
/// <see cref="Isbn"/> and <see cref="StarRating"/> were first mapped with value converters. That
/// compiles, and every unit test passed — but a converter collapses the value object into an opaque
/// scalar, so the provider cannot see the members inside it. <c>book.Isbn.Value</c> then becomes
/// untranslatable and catalogue search failed at run time, in the running system, on a query nobody
/// could have caught at build time.
/// </para>
/// <para>
/// These tests assert the shape of the model rather than the behaviour of a query, because that is
/// what actually distinguishes the two mappings. Asserting on a query would need a real database:
/// the in-memory provider translates member access on a converted type happily and would report the
/// broken mapping as working.
/// </para>
/// </summary>
[TestFixture]
public sealed class ValueObjectMappingTests
{
    [Test]
    public void Isbn_IsMappedAsAnOwnedTypeSoItsValueStaysQueryable()
    {
        using var context = TestDbContext.Create();

        var isbn = context.Model.FindEntityType(typeof(Book))!.FindNavigation(nameof(Book.Isbn));

        isbn.Should().NotBeNull("a value converter would leave no navigation and break every query on the ISBN");
        isbn!.ForeignKey.DeclaringEntityType.IsOwned().Should().BeTrue();
    }

    [Test]
    public void Isbn_StillOccupiesTheSingleIsbnColumn()
    {
        // Owning the type must not change the schema: the point of the fix was that it does not.
        using var context = TestDbContext.Create();

        var owned = context.Model.FindEntityType(typeof(Book))!
            .FindNavigation(nameof(Book.Isbn))!.TargetEntityType;

        owned.FindProperty(nameof(Isbn.Value))!
            .GetColumnName().Should().Be("isbn");
    }

    [Test]
    public void StarRating_IsMappedAsAnOwnedTypeSoItsStarsCanBeAveraged()
    {
        // The book rating is a database-side average over this member. A converter would make that
        // aggregate untranslatable, exactly as it did for the ISBN.
        using var context = TestDbContext.Create();

        var rating = context.Model.FindEntityType(typeof(Review))!.FindNavigation(nameof(Review.Rating));

        rating.Should().NotBeNull();
        rating!.ForeignKey.DeclaringEntityType.IsOwned().Should().BeTrue();
        rating.TargetEntityType.FindProperty(nameof(StarRating.Stars))!
            .GetColumnName().Should().Be("rating");
    }

    [Test]
    public void Money_IsMappedAsAComplexTypeSoItsCentsStayQueryable()
    {
        // Found the same way as the ISBN: a converter compiled, every unit test passed, and sorting
        // the catalogue by price returned 500 in the running system. Money is filtered and sorted on
        // in store and fines as well, so this guard covers more than one screen.
        using var context = TestDbContext.Create();

        var price = context.Model.FindEntityType(typeof(Book))!
            .FindComplexProperty(nameof(Book.RetailPrice));

        price.Should().NotBeNull("a value converter would hide Cents from the provider");
        price!.ComplexType.FindProperty(nameof(Money.Cents))!
            .GetColumnName().Should().Be("retail_price_cents");
    }

    [Test]
    public void TheBillingCycleAnchorIsStoredRatherThanDerived()
    {
        // BR-MBR-026. A cycle anchored on the 31st renews on 28 February and must return to the 31st
        // in March; deriving the anchor from the last renewal walks the billing day backwards.
        using var context = TestDbContext.Create();

        var cycle = context.Model.FindEntityType(typeof(Subscription))!
            .FindNavigation(nameof(Subscription.Cycle))!.TargetEntityType;

        cycle.FindProperty("AnchorDay")!.GetColumnName().Should().Be("cycle_anchor_day");
    }

    [Test]
    public void EveryGuidPrimaryKeyIsAssignedByTheDomain()
    {
        // Regression guard from Stage 1: EF treating a domain-assigned Guid key as store-generated
        // emitted an UPDATE instead of an INSERT, which surfaced as a 500 on token refresh.
        using var context = TestDbContext.Create();

        var storeGenerated = context.Model.GetEntityTypes()
            .Where(entity => !entity.IsOwned())
            .Select(entity => entity.FindPrimaryKey())
            .Where(key => key is { Properties.Count: 1 })
            .Select(key => key!.Properties[0])
            .Where(property => property.ClrType == typeof(Guid))
            .Where(property => property.ValueGenerated != ValueGenerated.Never)
            .Select(property => $"{property.DeclaringType.ClrType.Name}.{property.Name}")
            .ToList();

        storeGenerated.Should().BeEmpty();
    }
}
