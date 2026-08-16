using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Application.Abstractions.Messaging;
using Astrolabe.Application.Contracts.Membership;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Errors;
using Astrolabe.Domain.Features.Membership.Policies;
using Astrolabe.Domain.Features.Membership.Repositories;
using Astrolabe.Domain.Primitives;

namespace Astrolabe.Application.Features.Membership.Queries.QuotePlanChange;

public sealed class QuotePlanChangeQueryHandler(
    IMembershipUnitOfWork membership,
    ICurrentUser currentUser,
    IDateTimeProvider clock) : IQueryHandler<QuotePlanChangeQuery, PlanChangeQuoteDto>
{
    public async Task<Result<PlanChangeQuoteDto>> Handle(
        QuotePlanChangeQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } memberId)
        {
            return Result.Failure<PlanChangeQuoteDto>(MembershipErrors.SubscriptionNotFound);
        }

        var subscription = await membership.Subscriptions
            .GetActiveForMemberAsync(memberId, cancellationToken);

        if (subscription is null)
        {
            return Result.Failure<PlanChangeQuoteDto>(MembershipErrors.SubscriptionNotFound);
        }

        // The aggregate quotes; this handler only shapes. Computing the money here would put the
        // arithmetic beside the rules that constrain it instead of inside them.
        var quote = subscription.QuoteChange(request.TargetPlan, clock.UtcNow);

        if (quote.IsFailure)
        {
            return Result.Failure<PlanChangeQuoteDto>(quote.Error);
        }

        var losses = PlanChangePolicy.LossesOf(subscription.Plan, request.TargetPlan);

        return Result.Success(new PlanChangeQuoteDto(
            From: quote.Value.From.ToString(),
            To: quote.Value.To.ToString(),
            Direction: quote.Value.IsUpgrade ? "upgrade" : "downgrade",
            ChargeCents: (int)quote.Value.Charge.Cents,
            CreditCents: (int)quote.Value.Credit.Cents,
            AmountDueCents: (int)quote.Value.AmountDue.Cents,
            EffectiveOn: quote.Value.EffectiveOn,
            WhatYouLose: losses.Select(loss => loss.ToString()).ToList()));
    }
}
