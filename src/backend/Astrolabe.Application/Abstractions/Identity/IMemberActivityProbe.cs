using Astrolabe.Application.Contracts.Identity;

namespace Astrolabe.Application.Abstractions.Identity;

/// <summary>
/// Gathers what a member has been doing, across the domains that own the facts.
///
/// <para>
/// A seam rather than a set of direct queries because the answers live in <c>identity</c>'s
/// sessions, <c>reservations</c>, <c>billing</c> and <c>store</c>, and a query handler that reached
/// into four unit of work contracts to render one panel would be a handler nobody could test.
/// The same shape as <c>ILibraryObligationsProbe</c>.
/// </para>
/// </summary>
public interface IMemberActivityProbe
{
    Task<MemberActivity> GetAsync(Guid memberId, CancellationToken cancellationToken = default);
}
