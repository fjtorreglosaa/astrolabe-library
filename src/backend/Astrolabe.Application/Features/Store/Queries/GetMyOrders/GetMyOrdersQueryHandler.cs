using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Application.Shared.Store;
using Astrolabe.Domain.Features.Store.Errors;
using Astrolabe.Domain.Features.Store.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Store.Queries.GetMyOrders;

public sealed class GetMyOrdersQueryHandler(
    IStoreUnitOfWork store,
    ICurrentUser currentUser) : IQueryHandler<GetMyOrdersQuery, PagedResult<OrderDto>>
{
    public async Task<Result<PagedResult<OrderDto>>> Handle(
        GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<PagedResult<OrderDto>>(StoreErrors.OrderNotYours);
        }

        var page = await store.Orders.GetForMemberAsync(
            memberId, request.Page, request.PageSize, cancellationToken);

        return Result.Success(PagedResult<OrderDto>.Create(
            page.Items.Select(StorePricing.ToDto).ToList(),
            page.Page, page.PageSize, page.TotalCount));
    }
}
