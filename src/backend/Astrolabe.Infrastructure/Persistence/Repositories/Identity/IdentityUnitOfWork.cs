using Astrolabe.Domain.Features.Identity.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Identity;

/// <summary>
/// Composes the identity repositories over one shared context, so their staged work commits together.
/// </summary>
public sealed class IdentityUnitOfWork(
    AstrolabeDbContext context,
    IUserRepository users,
    IUserSessionRepository sessions,
    ISingleUseTokenRepository tokens) : UnitOfWorkBase(context), IIdentityUnitOfWork
{
    public IUserRepository Users { get; } = users;

    public IUserSessionRepository Sessions { get; } = sessions;

    public ISingleUseTokenRepository Tokens { get; } = tokens;
}
