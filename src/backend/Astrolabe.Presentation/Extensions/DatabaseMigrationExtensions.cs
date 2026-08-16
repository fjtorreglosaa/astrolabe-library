using Astrolabe.Infrastructure.Persistence;
using Astrolabe.Infrastructure.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Presentation.Extensions;

/// <summary>
/// Applies pending migrations as an explicit, logged startup step.
/// The application must never silently modify the schema, so EnsureCreated is not used.
/// See GUIDELINES.md sections 16 and 65.
/// </summary>
public static class DatabaseMigrationExtensions
{
    public static async Task ApplyMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var context = scope.ServiceProvider.GetRequiredService<AstrolabeDbContext>();

        var pending = (await context.Database.GetPendingMigrationsAsync()).ToArray();

        if (pending.Length == 0)
        {
            logger.LogInformation("Database schema is up to date. No migrations pending.");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s): {Migrations}",
            pending.Length, string.Join(", ", pending));

        await context.Database.MigrateAsync();

        logger.LogInformation("Migrations applied successfully.");
    }

    /// <summary>
    /// Applies reference data after migrations. Seeders are idempotent, so this runs on every
    /// startup and inserts only what is missing.
    /// </summary>
    public static async Task SeedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        // Order matters: demo members resolve their city from the network seed, and the
        // membership backfill needs those members to exist before it can subscribe them.
        await scope.ServiceProvider.GetRequiredService<NetworkSeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<DemoAccountSeeder>().SeedAsync();

        // After the accounts that sign in, because it checks addresses against them.
        await scope.ServiceProvider.GetRequiredService<DemoDirectorySeeder>().SeedAsync();
        await scope.ServiceProvider.GetRequiredService<MembershipSeeder>().SeedAsync();

        // Books resolve their branches by name from the network seed, so it must have run.
        await scope.ServiceProvider.GetRequiredService<CatalogSeeder>().SeedAsync();
    }
}
