using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.ResendVerificationForUser;

/// <summary>
/// A staff user resends the verification email for a pending account, from the directory.
///
/// <para>
/// Separate from <c>ResendVerificationCommand</c> although both issue the same link, and the
/// difference is not cosmetic. That one is anonymous and takes an <b>email</b>, so it must succeed
/// silently whatever it finds — anything else would let a stranger test which addresses have
/// accounts. This one is staff-only and takes an <b>identifier</b>, so it can and should say when
/// the account is not pending: an administrator who clicks resend deserves to know it did nothing.
/// </para>
/// </summary>
public sealed record ResendVerificationForUserCommand(Guid UserId) : ICommand;
