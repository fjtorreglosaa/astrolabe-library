using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Application.Features.Network.Commands.InviteAdmin;

/// <summary>
/// Invites a staff user. Implements BR-NET-008, BR-NET-013 and BR-NET-014.
/// </summary>
public sealed record InviteAdminCommand(
    string Email,
    string FullName,
    UserRole Role,
    IReadOnlyList<Guid> LibraryIds,
    string? Message) : ICommand<Guid>;
