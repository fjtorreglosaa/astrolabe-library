using Astrolabe.Domain.Features.Identity.Enums;

namespace Astrolabe.Presentation.Contracts.Network;

/// <summary>
/// The body of an administrator invitation. The libraries are applied on confirmation, never
/// now — BR-NET-014 — because the invitee has no account to attach them to until they accept.
/// </summary>
public sealed record InviteAdminRequest(
    string Email,
    string FullName,
    UserRole Role,
    IReadOnlyList<Guid> LibraryIds,
    string? Message);
