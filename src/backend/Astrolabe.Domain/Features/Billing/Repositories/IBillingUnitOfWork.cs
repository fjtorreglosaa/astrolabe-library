using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Billing.Repositories;

/// <summary>
/// The billing bounded context's unit of work.
///
/// A payment moves two things at once — the fine's status and a ledger entry — and they must commit
/// together or the ledger stops describing reality.
/// </summary>
public interface IBillingUnitOfWork : IUnitOfWork
{
    IFineRepository Fines { get; }

    ILedgerRepository Ledger { get; }

    IPaymentMethodRepository PaymentMethods { get; }

    IDeskPaymentRepository DeskPayments { get; }
}
