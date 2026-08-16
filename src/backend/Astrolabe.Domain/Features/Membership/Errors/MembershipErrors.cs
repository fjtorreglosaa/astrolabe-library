using Astrolabe.Domain.Primitives;

namespace Astrolabe.Domain.Features.Membership.Errors;

/// <summary>Reusable, strongly typed errors for the membership domain.</summary>
public static class MembershipErrors
{
    public static readonly Error SubscriptionNotFound =
        Error.NotFound("membership.subscription_not_found", "No active subscription was found.");

    public static readonly Error AlreadyOnThatPlan =
        Error.Conflict("membership.already_on_that_plan", "You are already on this plan.");

    public static readonly Error PaymentMethodRequired =
        Error.Validation("membership.payment_method_required",
            "Add a payment method before moving to a paid plan.");

    public static readonly Error NoScheduledChange =
        Error.Conflict("membership.no_scheduled_change", "There is no scheduled change to cancel.");

    public static readonly Error CityChangeLimitReached =
        Error.Conflict("membership.city_change_limit_reached",
            "You can change your city once per billing period. Try again after your next renewal.");

    public static readonly Error SubscriptionEnded =
        Error.Conflict("membership.subscription_ended", "This subscription is no longer active.");
}
