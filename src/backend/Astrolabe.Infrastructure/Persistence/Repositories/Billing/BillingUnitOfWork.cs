using Astrolabe.Domain.Features.Billing.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Billing;

/// <summary>
/// Composes the billing repositories over one shared context. A payment moves a fine and writes a
/// ledger entry, and they commit together or the ledger stops describing reality.
/// </summary>
public sealed class BillingUnitOfWork(
    AstrolabeDbContext context,
    IFineRepository fines,
    ILedgerRepository ledger,
    IPaymentMethodRepository paymentMethods,
    IDeskPaymentRepository deskPayments) : UnitOfWorkBase(context), IBillingUnitOfWork
{
    public IFineRepository Fines { get; } = fines;

    public ILedgerRepository Ledger { get; } = ledger;

    public IPaymentMethodRepository PaymentMethods { get; } = paymentMethods;

    public IDeskPaymentRepository DeskPayments { get; } = deskPayments;
}
