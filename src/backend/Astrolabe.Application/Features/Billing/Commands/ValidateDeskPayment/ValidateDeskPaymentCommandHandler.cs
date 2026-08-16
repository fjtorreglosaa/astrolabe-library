using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Features.Billing.ValueObjects;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Commands.ValidateDeskPayment;

public sealed class ValidateDeskPaymentCommandHandler(
    IBillingUnitOfWork billing,
    IAuditUnitOfWork audit,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<ValidateDeskPaymentCommand>
{
    public async Task<Result> Handle(
        ValidateDeskPaymentCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.Role is not { } role || !role.IsStaff())
        {
            return Result.Failure(NetworkErrors.StaffRequired);
        }

        var code = PaymentCode.Create(request.Code);

        if (code.IsFailure)
        {
            return Result.Failure(code.Error);
        }

        var payment = await billing.DeskPayments.GetByCodeAsync(code.Value.Value, cancellationToken);

        if (payment is null)
        {
            return Result.Failure(BillingErrors.DeskPaymentNotFound);
        }

        // BR-BIL-005. A librarian takes money at their own counter, not at somebody else's.
        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (!reach.Covers(payment.LibraryId))
        {
            return Result.Failure(BillingErrors.LibraryOutOfScope);
        }

        var now = clock.UtcNow;

        // The entity refuses an expired or already-resolved code. Two administrators reaching for
        // the same one therefore produce one validation and one clear refusal.
        var validated = payment.Validate(now);

        if (validated.IsFailure)
        {
            return validated;
        }

        // The fines are re-read now rather than trusted from issue time: BR-BIL-018 settles what the
        // code actually covers, and something may have changed since the code was printed.
        var fines = await billing.Fines.GetByDeskPaymentAsync(payment.Id, cancellationToken);

        var entries = new List<LedgerEntry>();

        foreach (var fine in fines)
        {
            var moved = fine.Settle(now);

            if (moved.IsSuccess && moved.Value)
            {
                entries.Add(LedgerEntry.Payment(
                    fine.MemberId, fine.Amount,
                    $"Paid at the desk — {fine.BookTitle}", fine.Id, now));
            }
        }

        await billing.Ledger.AddRangeAsync(entries, cancellationToken);

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "billing.desk_payment_validated", now,
                actorUserId: currentUser.UserId, subjectUserId: payment.MemberId,
                detail: $"{payment.Code} · {payment.Amount}"),
            cancellationToken);

        await billing.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
