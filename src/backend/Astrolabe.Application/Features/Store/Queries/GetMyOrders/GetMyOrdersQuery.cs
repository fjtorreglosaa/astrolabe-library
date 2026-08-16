using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Store;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Store.Queries.GetMyOrders;

/// <summary>The caller's own purchases. Implements BR-STR-016 by taking no identifier.</summary>
public sealed record GetMyOrdersQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<OrderDto>>;
