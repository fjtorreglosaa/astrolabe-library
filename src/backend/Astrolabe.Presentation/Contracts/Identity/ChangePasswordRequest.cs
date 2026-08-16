namespace Astrolabe.Presentation.Contracts.Identity;

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
