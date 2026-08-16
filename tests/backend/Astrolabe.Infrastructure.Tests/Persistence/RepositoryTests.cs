using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Infrastructure.Persistence.Repositories;
using Astrolabe.Infrastructure.Persistence.Repositories.Identity;
using Astrolabe.Infrastructure.Persistence.Repositories.Network;
using Astrolabe.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises the generic repository contract through a concrete repository. Each test gets a
/// uniquely named in-memory database so state never leaks, per SDD_PLIUS_STRATEGY.md section 9.1.
/// </summary>
[TestFixture]
public sealed class RepositoryTests
{
    private AstrolabeDbContext _context = null!;
    private CountryRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _context = TestDbContext.Create();
        _repository = new CountryRepository(_context);
    }

    [TearDown]
    public void TearDown() => _context.Dispose();

    private static Country ACountry(string name, string iso) =>
        Country.Create(Guid.NewGuid(), name, iso).Value;

    private async Task SeedAsync(params Country[] countries)
    {
        await _repository.AddRangeAsync(countries, TestContext.CurrentContext.CancellationToken);
        await _context.SaveChangesAsync(TestContext.CurrentContext.CancellationToken);
    }

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    // ---------- Writes ----------

    [Test]
    public async Task AddAsync_StagesOneEntity_AndPersistsOnSave()
    {
        var country = ACountry("Spain", "ES");

        await _repository.AddAsync(country, Ct);

        // Nothing is written until the unit of work commits.
        (await _repository.CountAsync(cancellationToken: Ct)).Should().Be(0);

        await _context.SaveChangesAsync(Ct);

        (await _repository.CountAsync(cancellationToken: Ct)).Should().Be(1);
    }

    [Test]
    public async Task AddRangeAsync_StagesManyEntities()
    {
        await SeedAsync(ACountry("Spain", "ES"), ACountry("Mexico", "MX"), ACountry("Canada", "CA"));

        (await _repository.CountAsync(cancellationToken: Ct)).Should().Be(3);
    }

    [Test]
    public async Task Remove_DeletesOnSave()
    {
        var country = ACountry("Spain", "ES");
        await SeedAsync(country);

        _repository.Remove(country);
        await _context.SaveChangesAsync(Ct);

        (await _repository.ExistsAsync(country.Id, Ct)).Should().BeFalse();
    }

    [Test]
    public async Task RemoveRange_DeletesManyOnSave()
    {
        var spain = ACountry("Spain", "ES");
        var mexico = ACountry("Mexico", "MX");
        await SeedAsync(spain, mexico, ACountry("Canada", "CA"));

        _repository.RemoveRange([spain, mexico]);
        await _context.SaveChangesAsync(Ct);

        (await _repository.CountAsync(cancellationToken: Ct)).Should().Be(1);
    }

    [Test]
    public void AddAsync_WithNull_Throws()
    {
        var act = async () => await _repository.AddAsync(null!, Ct);

        act.Should().ThrowAsync<ArgumentNullException>();
    }

    // ---------- Reads: single ----------

    [Test]
    public async Task GetByIdAsync_ReturnsTheEntity()
    {
        var country = ACountry("Spain", "ES");
        await SeedAsync(country);

        (await _repository.GetByIdAsync(country.Id, Ct))!.Name.Should().Be("Spain");
    }

    [Test]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        (await _repository.GetByIdAsync(Guid.NewGuid(), Ct)).Should().BeNull();
    }

    [Test]
    public async Task GetByFilterAsync_FindsByAnyPredicate()
    {
        await SeedAsync(ACountry("Spain", "ES"), ACountry("Mexico", "MX"));

        var found = await _repository.GetByFilterAsync(c => c.IsoCode == "MX", Ct);

        found!.Name.Should().Be("Mexico");
    }

    [Test]
    public async Task GetByFilterAsync_WhenNothingMatches_ReturnsNull()
    {
        await SeedAsync(ACountry("Spain", "ES"));

        (await _repository.GetByFilterAsync(c => c.IsoCode == "ZZ", Ct)).Should().BeNull();
    }

    [Test]
    public async Task GetByIdAsync_ReturnsATrackedEntity()
    {
        // The safe default: an entity you read can be mutated and saved. Returning an untracked
        // entity that a caller then modifies would silently lose the change.
        var country = ACountry("Spain", "ES");
        await SeedAsync(country);
        _context.ChangeTracker.Clear();

        var loaded = await _repository.GetByIdAsync(country.Id, Ct);
        loaded!.HideFromRegistration();
        await _context.SaveChangesAsync(Ct);

        _context.ChangeTracker.Clear();
        (await _repository.GetByIdAsync(country.Id, Ct))!.IsHiddenFromRegistration.Should().BeTrue();
    }

    // ---------- Reads: many ----------

    [Test]
    public async Task GetByIdsAsync_ReturnsOnlyTheRequestedEntities()
    {
        var spain = ACountry("Spain", "ES");
        var mexico = ACountry("Mexico", "MX");
        await SeedAsync(spain, mexico, ACountry("Canada", "CA"));

        var found = await _repository.GetByIdsAsync([spain.Id, mexico.Id], Ct);

        found.Select(c => c.IsoCode).Should().BeEquivalentTo(["ES", "MX"]);
    }

    [Test]
    public async Task GetByIdsAsync_WithNoIds_ReturnsEmptyWithoutQuerying()
    {
        await SeedAsync(ACountry("Spain", "ES"));

        (await _repository.GetByIdsAsync([], Ct)).Should().BeEmpty();
    }

    [Test]
    public async Task ListByFilterAsync_ReturnsEveryMatch()
    {
        var hidden = ACountry("Spain", "ES");
        hidden.HideFromRegistration();
        await SeedAsync(hidden, ACountry("Mexico", "MX"), ACountry("Canada", "CA"));

        var visible = await _repository.ListByFilterAsync(c => !c.IsHiddenFromRegistration, Ct);

        visible.Should().HaveCount(2);
    }

    // ---------- Paging ----------

    [Test]
    public async Task GetPagedAsync_ReturnsThePageAndTheFullTotal()
    {
        await SeedAsync(
            ACountry("Spain", "ES"), ACountry("Mexico", "MX"),
            ACountry("Canada", "CA"), ACountry("Colombia", "CO"), ACountry("Peru", "PE"));

        var page = await _repository.GetPagedAsync(page: 1, pageSize: 2, cancellationToken: Ct);

        page.Items.Should().HaveCount(2);
        page.TotalCount.Should().Be(5, "the total must ignore paging");
        page.TotalPages.Should().Be(3);
        page.HasPreviousPage.Should().BeFalse();
        page.HasNextPage.Should().BeTrue();
    }

    [Test]
    public async Task GetPagedAsync_PagesDoNotOverlap()
    {
        await SeedAsync(
            ACountry("Spain", "ES"), ACountry("Mexico", "MX"),
            ACountry("Canada", "CA"), ACountry("Colombia", "CO"));

        var first = await _repository.GetPagedAsync(1, 2, cancellationToken: Ct);
        var second = await _repository.GetPagedAsync(2, 2, cancellationToken: Ct);

        first.Items.Select(c => c.Id).Should().NotIntersectWith(second.Items.Select(c => c.Id));
    }

    [Test]
    public async Task GetPagedAsync_AppliesThePredicateBeforeCounting()
    {
        var hidden = ACountry("Spain", "ES");
        hidden.HideFromRegistration();
        await SeedAsync(hidden, ACountry("Mexico", "MX"), ACountry("Canada", "CA"));

        var page = await _repository.GetPagedAsync(1, 10, c => !c.IsHiddenFromRegistration, Ct);

        page.TotalCount.Should().Be(2);
    }

    [Test]
    public async Task GetPagedAsync_WhenNothingMatches_ReturnsAnEmptyPage()
    {
        await SeedAsync(ACountry("Spain", "ES"));

        var page = await _repository.GetPagedAsync(1, 10, c => c.IsoCode == "ZZ", Ct);

        page.IsEmpty.Should().BeTrue();
        page.TotalCount.Should().Be(0);
        page.TotalPages.Should().Be(0);
        page.HasNextPage.Should().BeFalse();
    }

    [Test]
    public async Task GetPagedAsync_ClampsAnInvalidPageRequest()
    {
        await SeedAsync(ACountry("Spain", "ES"));

        var page = await _repository.GetPagedAsync(page: 0, pageSize: -5, cancellationToken: Ct);

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(1);
    }

    [Test]
    public async Task GetPagedAsync_CapsAnOversizedPageRequest()
    {
        // Without the cap, a caller could ask for a million rows and defeat paging entirely.
        await SeedAsync(ACountry("Spain", "ES"));

        var page = await _repository.GetPagedAsync(1, pageSize: 100_000, cancellationToken: Ct);

        page.PageSize.Should().Be(100);
    }

    // ---------- Aggregates ----------

    [Test]
    public async Task ExistsAsync_AnswersByIdentifier()
    {
        var country = ACountry("Spain", "ES");
        await SeedAsync(country);

        (await _repository.ExistsAsync(country.Id, Ct)).Should().BeTrue();
        (await _repository.ExistsAsync(Guid.NewGuid(), Ct)).Should().BeFalse();
    }

    [Test]
    public async Task AnyAsync_AnswersByPredicate()
    {
        await SeedAsync(ACountry("Spain", "ES"));

        (await _repository.AnyAsync(c => c.IsoCode == "ES", Ct)).Should().BeTrue();
        (await _repository.AnyAsync(c => c.IsoCode == "ZZ", Ct)).Should().BeFalse();
    }

    [Test]
    public async Task CountAsync_WithoutAPredicate_CountsEverything()
    {
        await SeedAsync(ACountry("Spain", "ES"), ACountry("Mexico", "MX"));

        (await _repository.CountAsync(cancellationToken: Ct)).Should().Be(2);
    }

    [Test]
    public async Task CountAsync_WithAPredicate_CountsMatches()
    {
        var hidden = ACountry("Spain", "ES");
        hidden.HideFromRegistration();
        await SeedAsync(hidden, ACountry("Mexico", "MX"));

        (await _repository.CountAsync(c => c.IsHiddenFromRegistration, Ct)).Should().Be(1);
    }
}
