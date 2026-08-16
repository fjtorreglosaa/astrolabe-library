using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Billing.Queries.GetMyLedger;

/// <summary>The account statement: every movement, newest first.</summary>
public sealed record GetMyLedgerQuery(int Page = 1, int PageSize = 20)
    : IQuery<PagedResult<LedgerEntryDto>>;
