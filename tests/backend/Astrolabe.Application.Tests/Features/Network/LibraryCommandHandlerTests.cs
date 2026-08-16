using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Features.Network.Commands.CreateLibrary;
using Astrolabe.Application.Features.Network.Commands.DeactivateLibrary;
using Astrolabe.Application.Features.Network.Commands.DesignateHomeLibrary;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Features.Network.Repositories;
using Astrolabe.Application.Tests.TestSupport;
using FluentAssertions;
using Moq;

namespace Astrolabe.Application.Tests.Features.Network;

/// <summary>
/// Covers the library management handlers: BR-NET-002, BR-NET-003, BR-NET-005 and BR-NET-008.
/// </summary>
[TestFixture]
public sealed class LibraryCommandHandlerTests
{
    private NetworkUnitOfWorkMock _network = null!;
    private Mock<ICityRepository> _cities = null!;
    private Mock<ILibraryRepository> _libraries = null!;
    private Mock<ICurrentUser> _currentUser = null!;
    private Mock<ILibraryObligationsProbe> _obligations = null!;

    private static CancellationToken Ct => TestContext.CurrentContext.CancellationToken;

    [SetUp]
    public void SetUp()
    {
        _network = new NetworkUnitOfWorkMock();
        _cities = _network.Cities;
        _libraries = _network.Libraries;
        _currentUser = new Mock<ICurrentUser>();
        _obligations = new Mock<ILibraryObligationsProbe>();

        SignInAs(UserRole.SuperAdmin);
        _obligations.Setup(o => o.HasOpenObligationsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
    }

    private void SignInAs(UserRole role) => _currentUser.SetupGet(u => u.Role).Returns(role);

    private static City ACity(string name = "New York") =>
        City.Create(Guid.NewGuid(), Guid.NewGuid(), name).Value;

    private static Library ALibrary(Guid cityId, string name = "Midtown") =>
        Library.Create(Guid.NewGuid(), cityId, name).Value;

    private CreateLibraryCommandHandler CreateHandler() =>
        new(_network.Object, _currentUser.Object);

    private DeactivateLibraryCommandHandler DeactivateHandler() =>
        new(_network.Object, _obligations.Object, _currentUser.Object);

    private DesignateHomeLibraryCommandHandler DesignateHandler() =>
        new(_network.Object, _currentUser.Object);

    // ---------- BR-NET-008: authorization ----------

    [TestCase(UserRole.Basic)]
    [TestCase(UserRole.Plus)]
    [TestCase(UserRole.Max)]
    [TestCase(UserRole.Admin)]
    public async Task CreateLibrary_ByAnyoneButASuperAdmin_IsRefused(UserRole role)
    {
        // An Admin manages books and loans in their libraries; only a super administrator shapes
        // the network itself.
        SignInAs(role);

        var result = await CreateHandler().Handle(
            new CreateLibraryCommand(Guid.NewGuid(), "Midtown"), Ct);

        result.Error.Should().Be(NetworkErrors.SuperAdminRequired);
        _network.Saved.Should().Be(0);
    }

    [Test]
    public async Task DeactivateLibrary_ByAnAdmin_IsRefused()
    {
        SignInAs(UserRole.Admin);

        var result = await DeactivateHandler().Handle(
            new DeactivateLibraryCommand(Guid.NewGuid()), Ct);

        result.Error.Should().Be(NetworkErrors.SuperAdminRequired);
    }

    [Test]
    public async Task DesignateHomeLibrary_ByAnAdmin_IsRefused()
    {
        SignInAs(UserRole.Admin);

        var result = await DesignateHandler().Handle(
            new DesignateHomeLibraryCommand(Guid.NewGuid(), Guid.NewGuid()), Ct);

        result.Error.Should().Be(NetworkErrors.SuperAdminRequired);
    }

    // ---------- Creation ----------

    [Test]
    public async Task CreateLibrary_InAMissingCity_Fails()
    {
        _cities.Setup(c => c.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((City?)null);

        var result = await CreateHandler().Handle(
            new CreateLibraryCommand(Guid.NewGuid(), "Midtown"), Ct);

        result.Error.Should().Be(NetworkErrors.CityNotFound);
    }

    [Test]
    public async Task CreateLibrary_WithADuplicateNameInTheSameCity_Fails()
    {
        // BR-NET-002. Checked here so the caller gets a clean conflict rather than a constraint error.
        var city = ACity();
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);
        _libraries.Setup(l => l.ExistsWithNameInCityAsync(city.Id, "Midtown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(new CreateLibraryCommand(city.Id, "Midtown"), Ct);

        result.Error.Should().Be(NetworkErrors.LibraryNameTakenInCity);
    }

    [Test]
    public async Task CreateLibrary_TrimsTheNameBeforeCheckingForDuplicates()
    {
        var city = ACity();
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);
        _libraries.Setup(l => l.ExistsWithNameInCityAsync(city.Id, "Midtown", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await CreateHandler().Handle(new CreateLibraryCommand(city.Id, "  Midtown  "), Ct);

        result.Error.Should().Be(NetworkErrors.LibraryNameTakenInCity,
            "whitespace must not be a way to bypass the uniqueness rule");
    }

    [Test]
    public async Task CreateLibrary_WithoutAName_Fails()
    {
        var city = ACity();
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        var result = await CreateHandler().Handle(new CreateLibraryCommand(city.Id, "   "), Ct);

        result.Error.Should().Be(NetworkErrors.LibraryNameRequired);
    }

    [Test]
    public async Task CreateLibrary_TheFirstOneInACity_BecomesItsHomeLibrary()
    {
        // Guards BR-NET-003: adding a branch to a city that had none must not leave the city
        // without a home library.
        var city = ACity();
        city.HomeLibraryId.Should().BeNull();
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        var result = await CreateHandler().Handle(new CreateLibraryCommand(city.Id, "Midtown"), Ct);

        result.IsSuccess.Should().BeTrue();
        city.HomeLibraryId.Should().Be(result.Value);
    }

    [Test]
    public async Task CreateLibrary_WhenTheCityAlreadyHasAHome_LeavesItAlone()
    {
        var city = ACity();
        var existing = ALibrary(city.Id, "Midtown");
        city.DesignateHomeLibrary(existing);
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        await CreateHandler().Handle(new CreateLibraryCommand(city.Id, "Harlem"), Ct);

        city.HomeLibraryId.Should().Be(existing.Id);
    }

    [Test]
    public async Task CreateLibrary_PersistsOnce()
    {
        var city = ACity();
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        await CreateHandler().Handle(new CreateLibraryCommand(city.Id, "Midtown"), Ct);

        _libraries.Verify(l => l.AddAsync(It.IsAny<Library>(), It.IsAny<CancellationToken>()), Times.Once);
        _network.Saved.Should().Be(1);
    }

    // ---------- Deactivation, BR-NET-005 ----------

    [Test]
    public async Task DeactivateLibrary_ThatIsItsCitysHome_IsBlocked()
    {
        var city = ACity();
        var library = ALibrary(city.Id);
        city.DesignateHomeLibrary(library);
        _libraries.Setup(l => l.GetByIdAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync(library);
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        var result = await DeactivateHandler().Handle(new DeactivateLibraryCommand(library.Id), Ct);

        result.Error.Should().Be(NetworkErrors.CannotDeactivateHomeLibrary);
        library.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task DeactivateLibrary_WithOpenObligations_IsBlocked()
    {
        var city = ACity();
        var library = ALibrary(city.Id);
        _libraries.Setup(l => l.GetByIdAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync(library);
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);
        _obligations.Setup(o => o.HasOpenObligationsAsync(library.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await DeactivateHandler().Handle(new DeactivateLibraryCommand(library.Id), Ct);

        result.Error.Should().Be(NetworkErrors.LibraryHasOpenObligations);
        library.IsActive.Should().BeTrue();
    }

    [Test]
    public async Task DeactivateLibrary_WithNothingOutstanding_Succeeds()
    {
        var city = ACity();
        var library = ALibrary(city.Id);
        _libraries.Setup(l => l.GetByIdAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync(library);
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        var result = await DeactivateHandler().Handle(new DeactivateLibraryCommand(library.Id), Ct);

        result.IsSuccess.Should().BeTrue();
        library.IsActive.Should().BeFalse();
        _network.Saved.Should().Be(1);
    }

    [Test]
    public async Task DeactivateLibrary_DoesNotSaveWhenTheRuleBlocksIt()
    {
        var city = ACity();
        var library = ALibrary(city.Id);
        city.DesignateHomeLibrary(library);
        _libraries.Setup(l => l.GetByIdAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync(library);
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);

        await DeactivateHandler().Handle(new DeactivateLibraryCommand(library.Id), Ct);

        _network.Saved.Should().Be(0);
    }

    // ---------- Home library designation, BR-NET-003 ----------

    [Test]
    public async Task DesignateHomeLibrary_WithALibraryFromAnotherCity_Fails()
    {
        var city = ACity();
        var foreign = ALibrary(Guid.NewGuid(), "Loop");
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);
        _libraries.Setup(l => l.GetByIdAsync(foreign.Id, It.IsAny<CancellationToken>())).ReturnsAsync(foreign);

        var result = await DesignateHandler().Handle(
            new DesignateHomeLibraryCommand(city.Id, foreign.Id), Ct);

        result.Error.Should().Be(NetworkErrors.HomeLibraryNotInCity);
    }

    [Test]
    public async Task DesignateHomeLibrary_WithAnInactiveLibrary_Fails()
    {
        var city = ACity();
        var library = ALibrary(city.Id);
        library.Deactivate(isCityHomeLibrary: false, hasOpenObligations: false);
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);
        _libraries.Setup(l => l.GetByIdAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync(library);

        var result = await DesignateHandler().Handle(
            new DesignateHomeLibraryCommand(city.Id, library.Id), Ct);

        result.Error.Should().Be(NetworkErrors.HomeLibraryInactive);
    }

    [Test]
    public async Task DesignateHomeLibrary_Succeeds_AndPersists()
    {
        var city = ACity();
        var library = ALibrary(city.Id);
        _cities.Setup(c => c.GetByIdAsync(city.Id, It.IsAny<CancellationToken>())).ReturnsAsync(city);
        _libraries.Setup(l => l.GetByIdAsync(library.Id, It.IsAny<CancellationToken>())).ReturnsAsync(library);

        var result = await DesignateHandler().Handle(
            new DesignateHomeLibraryCommand(city.Id, library.Id), Ct);

        result.IsSuccess.Should().BeTrue();
        city.HomeLibraryId.Should().Be(library.Id);
        _network.Saved.Should().Be(1);
    }
}
