using Astrolabe.Application.Features.Billing.Commands.AssessFine;
using Astrolabe.Domain.Features.Reservations.Repositories;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Astrolabe.Infrastructure.Features.Billing;

/// <summary>
/// Sweeps late returns whose fine was never assessed. Implements the guarantee half of the accrual
/// design.
///
/// <para>
/// <c>AssessFineOnReturnHandler</c> prices a fine the moment a copy is checked in, but it runs after
/// the commit and may be lost — and our own rule bars a post-commit reaction from carrying a business
/// outcome alone. A lost event would otherwise be an unbilled fine that nobody ever notices. This job
/// is what makes that impossible; the handler is only what makes it fast.
/// </para>
/// <para>
/// Both call the same idempotent command, so the two overlapping costs one extra query and nothing
/// else. Per SDD+ section 9.1 the job dispatches through <see cref="ISender"/> rather than reaching
/// for repositories itself, takes its schedule through <c>IOptions&lt;T&gt;</c>, and never runs
/// concurrently with itself: one tick finishes before the next begins.
/// </para>
/// </summary>
public sealed class AssessOutstandingFinesJob(
    IServiceScopeFactory scopeFactory,
    IOptions<FineAccrualOptions> options,
    ILogger<AssessOutstandingFinesJob> logger) : BackgroundService
{
    private readonly FineAccrualOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation("Fine accrual sweep is disabled. No fines will be swept.");
            return;
        }

        logger.LogInformation(
            "Fine accrual sweep starting. Interval: {Interval}, batch size: {BatchSize}.",
            _options.Interval, _options.BatchSize);

        using var timer = new PeriodicTimer(_options.Interval);

        // Once at startup and then on the timer, so a deployment after a missed window catches up
        // immediately rather than waiting a day.
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
                // A failed sweep must not take the host down. The next tick retries, and the work is
                // idempotent, so nothing is double-billed.
                logger.LogError(exception, "Fine accrual sweep failed. It will retry on the next tick.");
            }
        }
        while (await SafeWaitAsync(timer, stoppingToken));
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        // A scope per tick, resolved inside it. The unit of work is scoped, and a scope is what keeps
        // one change tracker per sweep.
        using var scope = scopeFactory.CreateScope();

        var reservations = scope.ServiceProvider.GetRequiredService<IReservationRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var lateReturns = await reservations.GetLateReturnsAsync(_options.BatchSize, cancellationToken);

        var assessed = 0;

        foreach (var reservation in lateReturns)
        {
            var result = await sender.Send(new AssessFineCommand(reservation.Id), cancellationToken);

            if (result.IsFailure)
            {
                logger.LogWarning(
                    "Could not assess a fine for reservation {ReservationId}: {Error}.",
                    reservation.Id, result.Error.Code);
                continue;
            }

            // The command answers with the existing fine when there already is one, so this counts
            // only the ones this sweep actually had to create.
            if (result.Value is not null)
            {
                assessed++;
            }
        }

        if (assessed > 0)
        {
            // Logged at warning on purpose: every fine this job creates is one the event handler
            // should have created already, and a rising number means events are being lost.
            logger.LogWarning(
                "Fine accrual sweep assessed {Assessed} fine(s) the return handler had not. "
                + "Reviewed {Count} late return(s).",
                assessed, lateReturns.Count);
        }
        else
        {
            logger.LogInformation(
                "Fine accrual sweep reviewed {Count} late return(s); all were already assessed.",
                lateReturns.Count);
        }
    }

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
