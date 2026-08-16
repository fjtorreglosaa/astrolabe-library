namespace Astrolabe.Presentation.Contracts.Network;

/// <summary>The invitee chooses their own password here. Nobody ever sets one for them.</summary>
public sealed record AcceptInvitationRequest(string Token, string Password);
