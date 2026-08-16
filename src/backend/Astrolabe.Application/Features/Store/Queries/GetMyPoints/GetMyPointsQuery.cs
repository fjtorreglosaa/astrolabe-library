using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Store;

namespace Astrolabe.Application.Features.Store.Queries.GetMyPoints;

/// <summary>The caller's reward balance and how it got there.</summary>
public sealed record GetMyPointsQuery : IQuery<PointsSummaryDto>;
