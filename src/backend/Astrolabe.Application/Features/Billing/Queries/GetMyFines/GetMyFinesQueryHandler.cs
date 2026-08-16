using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Abstractions.Network;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Shared.Billing;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Queries.GetMyFines;

public sealed class GetMyFinesQueryHandler(
    IBillingUnitOfWork billing,
    ILibraryLocationProvider libraries,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<GetMyFinesQuery, FinesSummaryDto>
{
    public async Task<Result<FinesSummaryDto>> Handle(
        GetMyFinesQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<FinesSummaryDto>(BillingErrors.FineNotYours);
        }

        var fines = await billing.Fines.GetForMemberAsync(memberId, status: null, cancellationToken);
        var locations = await libraries.GetAllAsync(cancellationToken);
        var balance = await billing.Ledger.GetBalanceAsync(memberId, cancellationToken);

        // Split rather than summed into one number. Money held by a desk code is still owed, but it
        // is not payable by card — folding the two together would invite the member to pay twice.
        var outstanding = fines.Where(f => f.Status is FineStatus.Outstanding).ToList();
        var awaiting = fines.Where(f => f.Status is FineStatus.AwaitingValidation).ToList();

        var deskPayments = await billing.DeskPayments.GetForMemberAsync(memberId, cancellationToken);
        var now = clock.UtcNow;

        var open = deskPayments
            .Where(payment => payment.IsPendingAt(now))
            .Select(payment => BillingProjection.ToDto(
                payment,
                fines.Where(f => payment.FineIds.Contains(f.Id)).ToList(),
                memberName: string.Empty,
                locations,
                now))
            .ToList();

        return Result.Success(new FinesSummaryDto(
            OutstandingCents: (int)outstanding.Sum(f => f.Amount.Cents),
            AwaitingValidationCents: (int)awaiting.Sum(f => f.Amount.Cents),
            TotalOwedCents: (int)fines.Where(f => f.IsOwed).Sum(f => f.Amount.Cents),
            BalanceCents: (int)balance.Cents,
            Fines: fines.Select(f => BillingProjection.ToDto(f, locations)).ToList(),
            OpenDeskPayments: open));
    }
}
