using Astrolabe.Application.Abstractions.Messaging;

namespace Astrolabe.Application.Features.Identity.Commands.SignOut;

/// <summary>Ends the calling session only. Implements BR-IDN-027.</summary>
public sealed record SignOutCommand : ICommand;
