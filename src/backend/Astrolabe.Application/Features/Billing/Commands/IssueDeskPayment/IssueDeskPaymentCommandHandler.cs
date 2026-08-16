using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Shared.Billing;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Audit.Repositories;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Commands.IssueDeskPayment;

public sealed class IssueDeskPaymentCommandHandler(
    IBillingUnitOfWork billing,
    IAuditUnitOfWork audit,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : ICommandHandler<IssueDeskPaymentCommand, DeskPaymentDto>
{
    public async Task<Result<DeskPaymentDto>> Handle(
        IssueDeskPaymentCommand request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<DeskPaymentDto>(BillingErrors.FineNotYours);
        }

        if (request.FineIds.Count == 0)
        {
            return Result.Failure<DeskPaymentDto>(BillingErrors.NothingToPay);
        }

        var fines = await billing.Fines.GetByIdsForMemberAsync(
            memberId, request.FineIds, cancellationToken);

        if (fines.Count == 0)
        {
            return Result.Failure<DeskPaymentDto>(BillingErrors.FineNotFound);
        }

        // All the fines a code covers must belong to one library, because BR-BIL-005 lets only that
        // library's staff take the money. A code spanning two counters could be validated at
        // neither without one of them acting outside their scope.
        var libraryIds = fines.Select(f => f.LibraryId).Distinct().ToList();

        if (libraryIds.Count > 1)
        {
            return Result.Failure<DeskPaymentDto>(BillingErrors.FinesSpanLibraries);
        }

        var now = clock.UtcNow;
        var amount = Money.FromCents(fines.Sum(f => f.Amount.Cents));

        var deskPayment = DeskPayment.Issue(
            memberId, libraryIds[0], amount, fines.Select(f => f.Id), now);

        // Held, not settled. BR-BIL-017: the debt stands until a librarian takes the money, and
        // BR-BIL-021 stops it being paid by card in the meantime.
        foreach (var fine in fines)
        {
            var held = fine.Hold(deskPayment.Id);

            if (held.IsFailure)
            {
                return Result.Failure<DeskPaymentDto>(held.Error);
            }
        }

        await billing.DeskPayments.AddAsync(deskPayment, cancellationToken);

        await audit.Entries.AddAsync(
            AuditEntry.Record(
                "billing.desk_payment_issued", now,
                actorUserId: memberId, subjectUserId: memberId,
                detail: $"{deskPayment.Code} · {amount}"),
            cancellationToken);

        await billing.SaveChangesAsync(cancellationToken);

        var locations = await libraries.GetAllAsync(cancellationToken);

        return Result.Success(BillingProjection.ToDto(
            deskPayment, fines, memberName: string.Empty, locations, now));
    }
}
