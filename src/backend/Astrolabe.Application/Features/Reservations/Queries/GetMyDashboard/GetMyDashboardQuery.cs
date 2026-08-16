using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Reservations;

namespace Astrolabe.Application.Features.Reservations.Queries.GetMyDashboard;

/// <summary>The home screen: what the member holds, what is due, and what they read.</summary>
public sealed record GetMyDashboardQuery : IQuery<MemberDashboardDto>;
