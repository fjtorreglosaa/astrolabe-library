using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Features.Recommendations.Events;
using Astrolabe.Domain.Features.Recommendations.Repositories;
using MediatR;

namespace Astrolabe.Application.Features.Recommendations.Events;

/// <summary>
/// Drops the recommendation sets a library generated, the moment it switches off. BR-REC-012.
///
/// <para>
/// An event handler and not a line in the command, because "immediate" has to hold however the
/// library came to be switched off — and because the rule is about a consequence rather than a step
/// the outcome depends on. If this reaction were lost the member would see a stale personalised set
/// until it expired, which is untidy rather than wrong; the switch itself already committed.
/// </para>
/// </summary>
public sealed class EvictSetsWhenLibraryDisabledHandler(IRecommendationsUnitOfWork recommendations)
    : INotificationHandler<DomainEventNotification<LibraryAiDisabled>>
{
    public async Task Handle(
        DomainEventNotification<LibraryAiDisabled> notification, CancellationToken cancellationToken)
    {
        await recommendations.Sets.RemoveGeneratedByAsync(
            notification.DomainEvent.LibraryId, cancellationToken);

        await recommendations.SaveChangesAsync(cancellationToken);
    }
}
