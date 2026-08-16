using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Domain.Features.Billing.Enums;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Queries.GetDeskPayments;

/// <summary>
/// The desk queue. Implements BR-BIL-005: scoped to the libraries assigned to the caller, and the
/// whole network for a super administrator.
/// </summary>
public sealed record GetDeskPaymentsQuery(
    DeskPaymentStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<DeskPaymentDto>>;
