namespace Astrolabe.Presentation.Contracts.Identity;

public sealed record ResetPasswordRequest(string Token, string NewPassword);
