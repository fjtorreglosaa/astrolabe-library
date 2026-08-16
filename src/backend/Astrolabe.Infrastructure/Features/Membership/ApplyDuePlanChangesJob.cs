using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Features.Membership.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabe.Infrastructure.Features.Membership;

/// <summary>
/// Applies scheduled plan changes whose renewal date has passed, and rolls the billing cycle
/// forward. Implements the sweep half of BR-MBR-021.
///
/// <para>
/// The second of two deliberate mechanisms. <c>IEntitlementProvider</c> applies a due change on
/// read, so a member who returns after their renewal sees the right plan at once — but a member who
/// never signs in would keep an entitlement they stopped paying for indefinitely. This job closes
/// that gap. Relying on the read alone never downgrades a dormant member; relying on the job alone
/// leaves a window in which the member sees a stale plan. Both go through
/// <c>Subscription.ApplyDueChange</c>, which is idempotent, so overlapping is harmless.
/// </para>
/// <para>
/// A DI scope is created per tick rather than injecting a context factory: the unit of work is
/// scoped, and resolving it inside a scope is what keeps one change tracker per sweep. Injecting
/// <c>IDbContextFactory</c> would hand out a fresh context per repository and silently break it.
/// </para>
/// </summary>
public sealed class ApplyDuePlanChangesJob(
    IServiceScopeFactory scopeFactory,
    IOptions<PlanRenewalOptions> options,
    ILogger<ApplyDuePlanChangesJob> logger) : BackgroundService
{
    private readonly PlanRenewalOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Plan renewal sweep is disabled. No subscriptions will be swept.");
            return;
        }

        logger.LogInformation(
            "Plan renewal sweep starting. Interval: {Interval}, batch size: {BatchSize}.",
            _options.Interval, _options.BatchSize);

        using var timer = new PeriodicTimer(_options.Interval);

        // Run once at startup and then on the timer, so a deployment after a missed window catches
        // up immediately instead of waiting a whole interval.
        do
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // A failed sweep must not take the host down. The next tick retries, and the work
                // is idempotent, so nothing is double-applied.
                logger.LogError(exception, "Plan renewal sweep failed. It will retry on the next tick.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();

        var membership = scope.ServiceProvider.GetRequiredService<IMembershipUnitOfWork>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var now = clock.UtcNow;

        var due = await membership.Subscriptions.GetDueForRenewalAsync(
            now, _options.BatchSize, cancellationToken);

        if (due.Count == 0)
        {
            return;
        }

        var applied = 0;

        foreach (var subscription in due)
        {
            var result = subscription.ApplyDueChange(now);

            if (result.IsSuccess && result.Value is not null)
            {
                applied++;
            }
        }

        // One commit for the whole batch. They share a change tracker, so a partial sweep cannot
        // leave some subscriptions renewed and others not.
        await membership.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Plan renewal sweep processed {Count} subscription(s); {Applied} changed plan.",
            due.Count, applied);
    }

    /// <summary>Swallows the cancellation that shutdown raises, so stopping is not an error.</summary>
    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
