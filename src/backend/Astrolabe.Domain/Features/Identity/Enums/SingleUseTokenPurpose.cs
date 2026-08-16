namespace Astrolabe.Domain.Features.Identity.Enums;

/// <summary>What a single-use emailed token authorises.</summary>
public enum SingleUseTokenPurpose
{
    EmailVerification = 0,
    PasswordRecovery = 1
}
