using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Infrastructure.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Tests.Persistence;

/// <summary>
/// Smoke coverage for the context. Each test gets a uniquely named in-memory database so state
/// never leaks between tests, per SDD_PLIUS_STRATEGY.md section 9.1.
/// </summary>
[TestFixture]
public sealed class AstrolabeDbContextTests
{
    private static AstrolabeDbContext CreateContext()
    {
        return TestDbContext.Create();
    }

    [Test]
    public void Context_CanBeConstructed()
    {
        using var context = CreateContext();

        context.Should().NotBeNull();
    }

    [Test]
    public void Context_ImplementsUnitOfWork()
    {
        using var context = CreateContext();

        context.Should().BeAssignableTo<IUnitOfWork>();
    }

    [Test]
    public async Task SaveChangesAsync_WithNoPendingChanges_ReturnsZero()
    {
        using var context = CreateContext();

        var affected = await ((IUnitOfWork)context).SaveChangesAsync(TestContext.CurrentContext.CancellationToken);

        affected.Should().Be(0);
    }

    [Test]
    public void Schema_IsNamespacedForTheApplication()
    {
        // Guards against entities silently landing in the public schema.
        AstrolabeDbContext.Schema.Should().Be("astrolabe");
    }

    [Test]
    public void Model_MapsEveryNetworkEntity()
    {
        // Replaces the Stage 0 empty-model assertion. Guards against a configuration class being
        // added without its entity reaching the model, which fails silently at runtime.
        using var context = CreateContext();

        var mapped = context.Model.GetEntityTypes().Select(e => e.ClrType.Name).ToArray();

        mapped.Should().Contain(["Country", "City", "Library", "LibraryAssignment", "AdminInvitation"]);
    }

    [Test]
    public void Model_ExposesEveryNetworkDbSet()
    {
        using var context = CreateContext();

        context.Countries.Should().NotBeNull();
        context.Cities.Should().NotBeNull();
        context.Libraries.Should().NotBeNull();
        context.LibraryAssignments.Should().NotBeNull();
        context.AdminInvitations.Should().NotBeNull();
    }
}
