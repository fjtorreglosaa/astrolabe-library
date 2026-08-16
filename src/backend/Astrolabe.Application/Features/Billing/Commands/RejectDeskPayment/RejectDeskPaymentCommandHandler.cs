using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Features.Billing.ValueObjects;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Network.Errors;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Commands.RejectDeskPayment;

public sealed class RejectDeskPaymentCommandHandler(
    IBillingUnitOfWork billing,
    IAuditUnitOfWork audit,
    ILibraryScopeProvider scope,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<RejectDeskPaymentCommand>
{
    public async Task<Result> Handle(
        RejectDeskPaymentCommand request, CancellationToken cancellationToken)
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

        var reach = await scope.GetCurrentScopeAsync(cancellationToken);

        if (!reach.Covers(payment.LibraryId))
        {
            return Result.Failure(BillingErrors.LibraryOutOfScope);
        }

        var now = clock.UtcNow;
        var rejected = payment.Reject(request.Reason, now);

        if (rejected.IsFailure)
        {
            return rejected;
        }

        // BR-BIL-019. The debt was never settled — it was only held — so releasing it returns the
        // member to exactly where they were before they asked for a code. Nothing is forgiven.
        var fines = await billing.Fines.GetByDeskPaymentAsync(payment.Id, cancellationToken);

        foreach (var fine in fines)
        {
            fine.Release();
        }

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "billing.desk_payment_rejected", now,
                actorUserId: currentUser.UserId, subjectUserId: payment.MemberId,
                detail: $"{payment.Code} · {payment.RejectionReason}"),
            cancellationToken);

        await billing.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
