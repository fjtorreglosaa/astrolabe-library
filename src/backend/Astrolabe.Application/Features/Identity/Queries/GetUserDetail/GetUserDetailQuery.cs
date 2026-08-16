using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Identity;

namespace Astrolabe.Application.Features.Identity.Queries.GetUserDetail;

/// <summary>
/// One account, as the directory's detail panel renders it. Staff only, and scoped the same way the
/// listing is — an administrator must not reach an account sideways by knowing its identifier.
/// </summary>
public sealed record GetUserDetailQuery(Guid UserId) : IQuery<UserDetailDto>;
