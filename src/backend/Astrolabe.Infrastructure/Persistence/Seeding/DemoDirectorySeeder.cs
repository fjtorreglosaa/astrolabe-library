using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Membership.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Persistence.Seeding;

/// <summary>
/// The rest of the prototype's user directory. PLAN-001 Stage 8 asks for fourteen users, and
/// <see cref="DemoAccountSeeder"/> creates the three that sign in.
///
/// <para>
/// These eleven exist to make the administration screens demonstrable. A user directory with two
/// rows cannot show a status filter, a scope boundary or a blocked account, so it cannot be reviewed
/// — and every one of those is a rule somebody is meant to check at acceptance.
/// </para>
/// <para>
/// <b>Development only</b>, like the demo accounts and for the same reason: they share one
/// well-known password written down in the prototype.
/// </para>
/// </summary>
public sealed class DemoDirectorySeeder(
    AstrolabeDbContext context,
    IPasswordHasher passwordHasher,
    IHostEnvironment environment,
    ILogger<DemoDirectorySeeder> logger)
{
    private const string DemoPassword = "Testing1234*";

    /// <summary>
    /// Transcribed from the prototype's <c>USERS</c>, less the three that sign in. The statuses are
    /// deliberately varied — a blocked, a pending and a deleted account are what make the directory's
    /// filters worth looking at.
    /// </summary>
    private static readonly (string Email, string FullName, PlanTier Plan, string City, UserStatus Status)[] Directory =
    [
        ("alice.n@fastmail.com", "Alice Nakamura", PlanTier.Max, "New York", UserStatus.Active),
        ("t.iriarte@correo.mx", "Tomás Iriarte", PlanTier.Basic, "Chicago", UserStatus.Active),
        ("grace.abbott@mail.com", "Grace Abbott", PlanTier.Plus, "Austin", UserStatus.Blocked),
        ("yusuf.demir@mail.com", "Yusuf Demir", PlanTier.Max, "New York", UserStatus.Active),
        ("rosa.l@post.se", "Rosa Lindqvist", PlanTier.Basic, "Chicago", UserStatus.PendingVerification),
        ("elias.brandt@mail.de", "Elias Brandt", PlanTier.Plus, "Austin", UserStatus.Deleted),
        ("nadia.h@mail.com", "Nadia Haddad", PlanTier.Plus, "New York", UserStatus.Active),
        ("kwame.b@mail.com", "Kwame Boateng", PlanTier.Basic, "Chicago", UserStatus.Active),
        ("sofia.m@posta.it", "Sofia Marchetti", PlanTier.Max, "New York", UserStatus.Blocked),
        ("hana.suzuki@mail.jp", "Hana Suzuki", PlanTier.Plus, "Austin", UserStatus.Active),
        ("marcus@astrolabe.co", "Marcus Oyelaran", PlanTier.Basic, "Chicago", UserStatus.Active),
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogInformation(
                "The demo directory is not seeded outside development. Environment: {Environment}.",
                environment.EnvironmentName);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var hash = passwordHasher.Hash(DemoPassword);
        var seeded = 0;

        foreach (var (email, fullName, plan, cityName, status) in Directory)
        {
            var address = Email.Create(email);

            if (address.IsFailure)
            {
                throw new InvalidOperationException($"Demo directory address '{email}' is invalid.");
            }

            // Checked against every status, not only the live ones: a deleted demo account still
            // occupies its address, and re-seeding it on every start would multiply the row.
            if (await context.Users.AnyAsync(u => u.Email == address.Value, cancellationToken))
            {
                continue;
            }

            var city = await context.Cities
                .FirstOrDefaultAsync(c => c.Name == cityName, cancellationToken);

            if (city is null)
            {
                logger.LogWarning(
                    "Demo directory user {Email} needs city '{City}', which the network seed did "
                    + "not create. Skipped.", email, cityName);
                continue;
            }

            var user = User.Register(address.Value, hash, fullName, city.CountryId, city.Id, plan, now);

            if (user.IsFailure)
            {
                throw new InvalidOperationException($"Demo directory user is invalid: {user.Error.Message}");
            }

            // Driven to the state the prototype shows, through the aggregate's own transitions
            // rather than by setting a column — so a status the domain would refuse cannot be
            // seeded into existence.
            switch (status)
            {
                case UserStatus.Active:
                    user.Value.Verify(now);
                    break;
                case UserStatus.Blocked:
                    user.Value.Verify(now);
                    user.Value.Block(now);
                    break;
                case UserStatus.Deleted:
                    user.Value.Verify(now);
                    user.Value.Delete(now);
                    break;
                default:
                    // PendingVerification is where Register leaves them.
                    break;
            }

            user.Value.ClearDomainEvents();
            context.Users.Add(user.Value);

            // Opened here rather than left to MembershipSeeder's backfill, which can only assume the
            // free tier — the same reasoning as the demo accounts. A directory where every member is
            // Basic would not exercise the plan column it is meant to demonstrate.
            var subscription = Subscription.Start(user.Value.Id, plan, now);
            subscription.ClearDomainEvents();
            context.Subscriptions.Add(subscription);

            seeded++;
        }

        if (seeded == 0)
        {
            logger.LogInformation("The demo directory already exists. Nothing inserted.");
            return;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Seeded {Count} demo directory user(s) with a shared, publicly known password. "
            + "This must never happen outside development.",
            seeded);
    }
}
