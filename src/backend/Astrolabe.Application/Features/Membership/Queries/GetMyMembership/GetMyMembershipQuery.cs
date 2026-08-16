using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;

namespace Astrolabe.Application.Features.Membership.Queries.GetMyMembership;

/// <summary>The caller's own membership, as the settings screen renders it. BR-MBR-001, BR-MBR-021.</summary>
public sealed record GetMyMembershipQuery : IQuery<MembershipDto>;
