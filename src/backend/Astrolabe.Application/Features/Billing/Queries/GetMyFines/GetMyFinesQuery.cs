using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Billing;

namespace Astrolabe.Application.Features.Billing.Queries.GetMyFines;

/// <summary>The caller's own fines and balance. Implements BR-BIL-016 by taking no identifier.</summary>
public sealed record GetMyFinesQuery : IQuery<FinesSummaryDto>;
