using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Application.Shared.Billing;
using Astrolabe.Domain.Features.Billing.Errors;
using Astrolabe.Domain.Features.Billing.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Queries.GetMyLedger;

public sealed class GetMyLedgerQueryHandler(
    IBillingUnitOfWork billing,
    ICurrentUser currentUser) : IQueryHandler<GetMyLedgerQuery, PagedResult<LedgerEntryDto>>
{
    public async Task<Result<PagedResult<LedgerEntryDto>>> Handle(
        GetMyLedgerQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<PagedResult<LedgerEntryDto>>(BillingErrors.FineNotYours);
        }

        var page = await billing.Ledger.GetForMemberAsync(
            memberId, request.Page, request.PageSize, cancellationToken);

        return Result.Success(PagedResult<LedgerEntryDto>.Create(
            page.Items.Select(BillingProjection.ToDto).ToList(),
            page.Page, page.PageSize, page.TotalCount));
    }
}
