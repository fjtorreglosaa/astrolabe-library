using Astrolabe.Domain.Features.Membership.Repositories;

namespace Astrolabe.Infrastructure.Persistence.Repositories.Membership;

/// <summary>
/// Composes the membership repositories over one shared context, so their staged work commits
/// together.
/// </summary>
public sealed class MembershipUnitOfWork(
    AstrolabeDbContext context,
    ISubscriptionRepository subscriptions) : UnitOfWorkBase(context), IMembershipUnitOfWork
{
    public ISubscriptionRepository Subscriptions { get; } = subscriptions;
}
