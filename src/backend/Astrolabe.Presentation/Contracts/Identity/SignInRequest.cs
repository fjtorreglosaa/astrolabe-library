namespace Astrolabe.Presentation.Contracts.Identity;

public sealed record SignInRequest(string Email, string Password, string? DeviceId);
