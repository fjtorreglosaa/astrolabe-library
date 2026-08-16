using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Identity.Repositories;

/// <summary>
/// The identity bounded context's unit of work.
///
/// <para>
/// It exposes only this context's repositories, which is the whole point of scoping it per context
/// rather than having one global unit of work: a handler in <c>identity</c> never sees the
/// repositories of <c>billing</c> or <c>catalog</c>, so the contexts stay decoupled even though the
/// dependency count drops.
/// </para>
///
/// <para>
/// Every repository reachable from here shares one change tracker, so a single
/// <see cref="IUnitOfWork.SaveChangesAsync"/> commits all of their staged work atomically. That
/// guarantee is now structural rather than a consequence of how the container happens to be wired.
/// </para>
/// </summary>
public interface IIdentityUnitOfWork : IUnitOfWork
{
    IUserRepository Users { get; }

    IUserSessionRepository Sessions { get; }

    ISingleUseTokenRepository Tokens { get; }

    IAuditRepository Audit { get; }
}
