using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.GrantSuperAdmin;

/// <summary>
/// Raises an administrator to super administrator. The prototype's <c>elevate</c>, and the "grant
/// extended powers" half of BR-NET-008.
///
/// <para>
/// There is deliberately no command to reverse this. BR-NET-012 forbids a super administrator from
/// revoking their own role so the network is never left without one, and a demotion command would
/// let two of them undo each other and reach the same empty state by another route. Demoting is
/// <c>RevokeAdminCommand</c>, which already refuses a super administrator.
/// </para>
/// </summary>
public sealed record GrantSuperAdminCommand(Guid UserId) : ICommand;
