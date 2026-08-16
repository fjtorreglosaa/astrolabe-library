using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;

namespace Astrolabe.Application.Features.Network.Queries.GetAdminTeam;

/// <summary>The staff team and their libraries. Super administrator only, per BR-NET-008.</summary>
public sealed record GetAdminTeamQuery : IQuery<IReadOnlyList<AdminDto>>;
