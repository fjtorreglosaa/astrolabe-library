using System.Reflection;
using Astrolabe.Application.Abstractions.Events;
using Astrolabe.Domain.Abstractions;
using Astrolabe.Domain.Abstractions.Persistence;
using Astrolabe.Domain.Features.Audit.Entities;
using Astrolabe.Domain.Features.Billing.Entities;
using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Identity.Entities;
using Astrolabe.Domain.Features.Membership.Entities;
using Astrolabe.Domain.Features.Network.Entities;
using Astrolabe.Domain.Features.Reservations.Entities;
using Microsoft.EntityFrameworkCore;

namespace Astrolabe.Infrastructure.Persistence;

/// <summary>
/// The application database context. Entity configuration lives in dedicated configuration classes
/// under <c>Persistence/Configurations</c> and is discovered by assembly scan, never inline here.
/// See GUIDELINES.md section 16.
/// </summary>
public sealed class AstrolabeDbContext(
    DbContextOptions<AstrolabeDbContext> options,
    IDomainEventDispatcher domainEventDispatcher)
    : DbContext(options), IUnitOfWork
{
    /// <summary>The schema every table lives in.</summary>
    public const string Schema = "astrolabe";

    public DbSet<Country> Countries => Set<Country>();

    public DbSet<City> Cities => Set<City>();

    public DbSet<Library> Libraries => Set<Library>();

    public DbSet<LibraryAssignment> LibraryAssignments => Set<LibraryAssignment>();

    public DbSet<AdminInvitation> AdminInvitations => Set<AdminInvitation>();

    public DbSet<User> Users => Set<User>();

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<SingleUseToken> SingleUseTokens => Set<SingleUseToken>();

    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    public DbSet<Review> Reviews => Set<Review>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<Fine> Fines => Set<Fine>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();

    public DbSet<DeskPayment> DeskPayments => Set<DeskPayment>();

    /// <summary>
    /// Commits, then publishes whatever the aggregates raised.
    ///
    /// <para>
    /// Events are collected and cleared <b>before</b> saving, and dispatched <b>after</b>: collecting
    /// first stops a reaction that saves again from re-publishing the same events, and dispatching
    /// after means no reaction can observe a change that was rolled back.
    /// </para>
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot>()
            .Where(entry => entry.Entity.DomainEvents.Count > 0)
            .Select(entry => entry.Entity)
            .ToList();

        var domainEvents = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        int affected;

        try
        {
            affected = await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            // Translated so the Application layer can react to a lost race without referencing EF
            // Core. Left unhandled it would surface as a 500, which tells a client nothing it can
            // act on.
            throw new ConcurrencyConflictException(
                "The record was modified by another request before this one committed.", exception);
        }

        if (domainEvents.Count > 0)
        {
            await domainEventDispatcher.DispatchAsync(domainEvents, cancellationToken);
        }

        return affected;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Identifiers are assigned by the domain, never by the database.
        //
        // Without this, EF's convention treats a Guid key as store-generated and therefore reads a
        // non-default value as "this row already exists", emitting an UPDATE where an INSERT was
        // meant. The symptom is silent: the write affects zero rows and surfaces later as a
        // concurrency failure on an unrelated entity.
        foreach (var key in modelBuilder.Model
                     .GetEntityTypes()
                     .Select(entityType => entityType.FindPrimaryKey())
                     .OfType<Microsoft.EntityFrameworkCore.Metadata.IMutableKey>())
        {
            foreach (var property in key.Properties.Where(p => p.ClrType == typeof(Guid)))
            {
                property.ValueGenerated = Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never;
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Runs work inside an explicit transaction, committing on success and rolling back on any
    /// exception.
    ///
    /// <para>
    /// Uses the configured execution strategy so a retry replays the <em>whole</em> transaction. A
    /// bare <c>BeginTransaction</c> would break under connection resiliency: a retry would resume
    /// mid-transaction on a connection that no longer has one.
    /// </para>
    ///
    /// <para>
    /// If a transaction is already open — a nested call — the work simply joins it rather than
    /// opening a second one, which PostgreSQL would reject.
    /// </para>
    /// </summary>
    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (Database.CurrentTransaction is not null)
        {
            return await operation(cancellationToken);
        }

        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async token =>
        {
            await using var transaction = await Database.BeginTransactionAsync(token);

            var result = await operation(token);

            await SaveChangesAsync(token);
            await transaction.CommitAsync(token);

            return result;
        }, cancellationToken);
    }

    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await ExecuteInTransactionAsync<object?>(async token =>
        {
            await operation(token);
            return null;
        }, cancellationToken);
    }
}
