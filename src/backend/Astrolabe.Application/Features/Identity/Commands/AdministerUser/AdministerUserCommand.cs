using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.AdministerUser;

/// <summary>
/// Blocks, unblocks, deletes or restores an account from the staff directory.
///
/// Implements BR-IDN-007, BR-IDN-008 and the directory half of BR-NET-006, BR-NET-008 and
/// BR-NET-010. No actor identifier: it comes from the token, never from the payload.
/// </summary>
public sealed record AdministerUserCommand(
    Guid UserId,
    UserAdministrationAction Action) : ICommand;
