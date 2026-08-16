using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Network;

namespace Astrolabe.Application.Features.Network.Queries.GetMyScope;

/// <summary>Which libraries the calling staff user may act on. Implements BR-NET-006 and BR-NET-010.</summary>
public sealed record GetMyScopeQuery : IQuery<LibraryScopeDto>;
