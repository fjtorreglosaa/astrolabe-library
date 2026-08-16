using Astrolabe.Domain.Abstractions.Persistence;

namespace Astrolabe.Domain.Features.Membership.Repositories;

/// <summary>
/// The membership bounded context's unit of work. Exposes only this context's repositories.
/// See <c>IIdentityUnitOfWork</c> for the rationale.
/// </summary>
public interface IMembershipUnitOfWork : IUnitOfWork
{
    ISubscriptionRepository Subscriptions { get; }
}
