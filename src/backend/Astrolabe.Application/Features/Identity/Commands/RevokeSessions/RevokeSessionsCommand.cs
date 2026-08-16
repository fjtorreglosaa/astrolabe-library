using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.RevokeSessions;

/// <summary>
/// Ends one, several, all other, or all sessions. Implements BR-IDN-023, BR-IDN-024 and BR-IDN-025.
/// Returns how many were ended.
/// </summary>
public sealed record RevokeSessionsCommand(
    RevocationScope Scope,
    IReadOnlyList<Guid>? SessionIds = null) : ICommand<int>;
