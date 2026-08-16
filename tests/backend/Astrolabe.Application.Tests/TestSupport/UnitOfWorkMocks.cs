using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Features.Catalog.Repositories;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Features.Reservations.Repositories;
using Astrolabe.Domain.Features.Store.Repositories;
using Astrolabe.Domain.Features.Identity.Repositories;
using Astrolabe.Domain.Features.Network.Repositories;
using Moq;

namespace Astrolabe.Application.Tests.TestSupport;

/// <summary>
/// Builds a mocked unit of work backed by mocked repositories.
///
/// A test still stubs the individual repositories it cares about; this only wires them into the
/// unit of work so the handler can reach them, and exposes <see cref="Saved"/> so a test can assert
/// whether the operation committed.
/// </summary>
public sealed class IdentityUnitOfWorkMock
{
    public IdentityUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Users).Returns(() => Users.Object);
        Mock.SetupGet(u => u.Sessions).Returns(() => Sessions.Object);
        Mock.SetupGet(u => u.Tokens).Returns(() => Tokens.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);
    }

    public Mock<IIdentityUnitOfWork> Mock { get; } = new();

    public Mock<IUserRepository> Users { get; } = new();

    public Mock<IUserSessionRepository> Sessions { get; } = new();

    public Mock<ISingleUseTokenRepository> Tokens { get; } = new();

    /// <summary>How many times the operation committed.</summary>
    public int Saved { get; private set; }

    public IIdentityUnitOfWork Object => Mock.Object;
}

/// <summary>
/// The audit trail, mocked separately now that it is its own bounded context. A handler that writes
/// an entry injects this alongside its own unit of work.
/// </summary>
public sealed class AuditUnitOfWorkMock
{
    public AuditUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Entries).Returns(() => Entries.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);
    }

    public Mock<IAuditUnitOfWork> Mock { get; } = new();

    public Mock<IAuditRepository> Entries { get; } = new();

    public int Saved { get; private set; }

    public IAuditUnitOfWork Object => Mock.Object;
}

public sealed class NetworkUnitOfWorkMock
{
    public NetworkUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Countries).Returns(() => Countries.Object);
        Mock.SetupGet(u => u.Cities).Returns(() => Cities.Object);
        Mock.SetupGet(u => u.Libraries).Returns(() => Libraries.Object);
        Mock.SetupGet(u => u.Assignments).Returns(() => Assignments.Object);
        Mock.SetupGet(u => u.Invitations).Returns(() => Invitations.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);
    }

    public Mock<INetworkUnitOfWork> Mock { get; } = new();

    public Mock<ICountryRepository> Countries { get; } = new();

    public Mock<ICityRepository> Cities { get; } = new();

    public Mock<ILibraryRepository> Libraries { get; } = new();

    public Mock<ILibraryAssignmentRepository> Assignments { get; } = new();

    public Mock<IAdminInvitationRepository> Invitations { get; } = new();

    public int Saved { get; private set; }

    public INetworkUnitOfWork Object => Mock.Object;
}

/// <summary>The membership context, for the plan handlers.</summary>
public sealed class MembershipUnitOfWorkMock
{
    public MembershipUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Subscriptions).Returns(() => Subscriptions.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);
    }

    public Mock<IMembershipUnitOfWork> Mock { get; } = new();

    public Mock<ISubscriptionRepository> Subscriptions { get; } = new();

    public int Saved { get; private set; }

    public IMembershipUnitOfWork Object => Mock.Object;
}

/// <summary>The catalog context, for the book and review handlers.</summary>
public sealed class CatalogUnitOfWorkMock
{
    public CatalogUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Books).Returns(() => Books.Object);
        Mock.SetupGet(u => u.Reviews).Returns(() => Reviews.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);
    }

    public Mock<ICatalogUnitOfWork> Mock { get; } = new();

    public Mock<IBookRepository> Books { get; } = new();

    public Mock<IReviewRepository> Reviews { get; } = new();

    public int Saved { get; private set; }

    public ICatalogUnitOfWork Object => Mock.Object;
}

/// <summary>
/// The reservations context. It exposes the book repository as well as its own, because taking a
/// copy and recording who took it is one atomic fact.
/// </summary>
public sealed class ReservationUnitOfWorkMock
{
    public ReservationUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Reservations).Returns(() => Reservations.Object);
        Mock.SetupGet(u => u.Books).Returns(() => Books.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);
    }

    public Mock<IReservationUnitOfWork> Mock { get; } = new();

    public Mock<IReservationRepository> Reservations { get; } = new();

    public Mock<IBookRepository> Books { get; } = new();

    public int Saved { get; private set; }

    public IReservationUnitOfWork Object => Mock.Object;
}

/// <summary>The billing context, for the fine, payment and desk handlers.</summary>
public sealed class BillingUnitOfWorkMock
{
    public BillingUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Fines).Returns(() => Fines.Object);
        Mock.SetupGet(u => u.Ledger).Returns(() => Ledger.Object);
        Mock.SetupGet(u => u.PaymentMethods).Returns(() => PaymentMethods.Object);
        Mock.SetupGet(u => u.DeskPayments).Returns(() => DeskPayments.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);

        // Empty by default, so a test that does not arrange fines gets "none" rather than a null.
        Fines.Setup(r => r.GetByIdsForMemberAsync(
                It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        Fines.Setup(r => r.GetByDeskPaymentAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
        PaymentMethods.Setup(r => r.GetForMemberAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    public Mock<IBillingUnitOfWork> Mock { get; } = new();

    public Mock<IFineRepository> Fines { get; } = new();

    public Mock<ILedgerRepository> Ledger { get; } = new();

    public Mock<IPaymentMethodRepository> PaymentMethods { get; } = new();

    public Mock<IDeskPaymentRepository> DeskPayments { get; } = new();

    public int Saved { get; private set; }

    public IBillingUnitOfWork Object => Mock.Object;
}

/// <summary>The store context, alongside the catalogue it prices from.</summary>
public sealed class StoreUnitOfWorkMock
{
    public StoreUnitOfWorkMock()
    {
        Mock.SetupGet(u => u.Orders).Returns(() => Orders.Object);
        Mock.SetupGet(u => u.Points).Returns(() => Points.Object);
        Mock.SetupGet(u => u.Books).Returns(() => Books.Object);
        Mock.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1)
            .Callback(() => Saved++);

        Books.Setup(r => r.GetByIdsWithCopiesAsync(
                It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    public Mock<IStoreUnitOfWork> Mock { get; } = new();

    public Mock<IOrderRepository> Orders { get; } = new();

    public Mock<IPointsRepository> Points { get; } = new();

    public Mock<IBookRepository> Books { get; } = new();

    public int Saved { get; private set; }

    public IStoreUnitOfWork Object => Mock.Object;
}
