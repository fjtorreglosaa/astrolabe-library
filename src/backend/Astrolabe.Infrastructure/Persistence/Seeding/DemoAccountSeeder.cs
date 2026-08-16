using Astrolabe.Application.Abstractions.Identity;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Identity.Enums;
using Astrolabe.Domain.Features.Identity.ValueObjects;
using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the three demo accounts from the approved prototype. Idempotent: it inserts only what is
/// missing.
///
/// <para>
/// <b>Development only.</b> It refuses to run outside a development environment, because these
/// accounts share one well-known password that is written down in the prototype. Seeding them into
/// a deployed environment would hand anyone who read the repository a super administrator.
/// </para>
/// </summary>
public sealed class DemoAccountSeeder(
    AstrolabeDbContext context,
    IPasswordHasher passwordHasher,
    IHostEnvironment environment,
    ILogger<DemoAccountSeeder> logger)
{
    /// <summary>The shared password from the prototype's sign-in screen.</summary>
    private const string DemoPassword = "Testing1234*";

    private static readonly (string Email, string FullName, UserRole Role, string? HomeCity)[] Accounts =
    [
        ("fjtorreglosaa@gmail.com", "Francisco Torreglosa", UserRole.Plus, "New York"),
        ("admin@astrolabe.co", "Dana Whitfield", UserRole.Admin, null),
        ("super@astrolabe.co", "Francisco Torreglosa", UserRole.SuperAdmin, null)
    ];

    /// <summary>The libraries the demo administrator manages, per the prototype.</summary>
    private static readonly string[] DemoAdminLibraries = ["Midtown", "Harlem"];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!environment.IsDevelopment())
        {
            logger.LogInformation(
                "Demo accounts are not seeded outside development. Environment: {Environment}.",
                environment.EnvironmentName);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var hash = passwordHasher.Hash(DemoPassword);
        var seeded = 0;

        foreach (var (email, fullName, role, homeCity) in Accounts)
        {
            var address = Email.Create(email);

            if (address.IsFailure)
            {
                throw new InvalidOperationException($"Demo account address '{email}' is invalid.");
            }

            if (await context.Users.AnyAsync(u => u.Email == address.Value, cancellationToken))
            {
                continue;
            }

            var user = role.IsMember()
                ? await CreateMemberAsync(address.Value, fullName, role, homeCity!, hash, now, cancellationToken)
                : CreateStaff(address.Value, fullName, role, hash, now);

            context.Users.Add(user);
            seeded++;

            if (role is UserRole.Admin)
            {
                await AssignDemoLibrariesAsync(user.Id, now, cancellationToken);
            }
        }

        if (seeded == 0)
        {
            logger.LogInformation("Demo accounts already exist. Nothing inserted.");
            return;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogWarning(
            "Seeded {Count} demo account(s) with a shared, publicly known password. "
            + "This must never happen outside development.",
            seeded);
    }

    private async Task<User> CreateMemberAsync(
        Email email, string fullName, UserRole plan, string cityName,
        PasswordHash hash, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var city = await context.Cities.FirstOrDefaultAsync(c => c.Name == cityName, cancellationToken)
            ?? throw new InvalidOperationException(
                $"Demo member needs city '{cityName}', which the network seed did not create.");

        var user = User.Register(email, hash, fullName, city.CountryId, city.Id, plan, now);

        if (user.IsFailure)
        {
            throw new InvalidOperationException($"Demo member is invalid: {user.Error.Message}");
        }

        // Verified immediately: a demo account that cannot sign in is useless, and the prototype
        // presents all three as ready to use.
        user.Value.Verify(now);
        user.Value.ClearDomainEvents();

        return user.Value;
    }

    private static User CreateStaff(
        Email email, string fullName, UserRole role, PasswordHash hash, DateTimeOffset now)
    {
        var user = User.Invite(email, fullName, role, now);

        if (user.IsFailure)
        {
            throw new InvalidOperationException($"Demo staff account is invalid: {user.Error.Message}");
        }

        user.Value.AcceptInvitation(hash, now);
        user.Value.ClearDomainEvents();

        return user.Value;
    }

    private async Task AssignDemoLibrariesAsync(
        Guid adminUserId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var libraries = await context.Libraries
            .Where(l => DemoAdminLibraries.Contains(l.Name))
            .ToListAsync(cancellationToken);

        foreach (var library in libraries)
        {
            var assignment = LibraryAssignment.Grant(
                Guid.NewGuid(), adminUserId, library.Id, adminUserId, now);

            assignment.ClearDomainEvents();
            context.LibraryAssignments.Add(assignment);
        }
    }
}
