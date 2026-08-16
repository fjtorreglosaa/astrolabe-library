using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;

namespace Astrolabe.Application.Features.Identity.Queries.GetCurrentUser;

/// <summary>The signed-in user, as the shell needs them to render.</summary>
public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;
