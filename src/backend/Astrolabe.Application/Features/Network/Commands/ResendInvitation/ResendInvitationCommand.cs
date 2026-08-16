using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Network.Commands.ResendInvitation;

/// <summary>
/// Issues a fresh invitation to a staff account that has not accepted yet. Implements BR-NET-015.
///
/// <para>
/// Takes the <b>user</b> rather than the invitation, because that is what the console has in front
/// of it — a row in the administrator team — and because BR-NET-015 requires every outstanding
/// invitation for that account to be invalidated, not just the one somebody happened to name.
/// </para>
/// </summary>
public sealed record ResendInvitationCommand(Guid UserId) : ICommand<Guid>;
