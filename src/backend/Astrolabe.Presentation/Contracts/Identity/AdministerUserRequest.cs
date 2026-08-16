using Astrolabe.Application.Features.Identity.Commands.AdministerUser;

namespace Astrolabe.Presentation.Contracts.Identity;

/// <summary>
/// The body of a directory action. The actor comes from the token, never from the payload.
/// </summary>
public sealed record AdministerUserRequest(UserAdministrationAction Action);
