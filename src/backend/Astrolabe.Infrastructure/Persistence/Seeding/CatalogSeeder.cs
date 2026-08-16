using Astrolabe.Domain.Features.Catalog.Entities;
using Astrolabe.Domain.Features.Catalog.Enums;
using Astrolabe.Domain.Features.Catalog.ValueObjects;
using Astrolabe.Domain.Features.Membership.Enums;
using Astrolabe.Domain.Primitives;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the twelve books and their stock from the approved prototype. Idempotent: it inserts only
/// the books that are missing, matched on ISBN.
///
/// <para>
/// The distribution is copied exactly rather than generated, because it is what makes every branch
/// of the access rule reachable in a demo. <i>The Savage Detectives</i> holds zero copies, which is
/// what makes "All copies out" observable without editing data; <i>Discipline and Punish</i> is a
/// Max title held only in New York, which is what makes both "Not in Basic plan" and "Not in
/// {city}" observable. Rebalancing this data would quietly remove those cases.
/// </para>
/// </summary>
public sealed class CatalogSeeder(
    AstrolabeDbContext context,
    ILogger<CatalogSeeder> logger)
{
    /// <summary>
    /// One branch's holding. <c>Available</c> is the prototype's per-branch figure, which is the one
    /// the access rule reads. <c>Total</c> defaults to it: the prototype records a total only at book
    /// level ("4 / 6") and never says how it splits across branches, so inventing a split would be
    /// fabricated data. It is stated explicitly only where the book has a single branch and the two
    /// figures are therefore unambiguous.
    /// </summary>
    private sealed record SeedStock(string City, string Branch, int Available, int? Total = null);

    private sealed record SeedBook(
        string Isbn,
        string Title,
        string Author,
        Genre Genre,
        PlanTier Tier,
        int PriceCents,
        SeedStock[] Stock);

    private static readonly SeedBook[] Books =
    [
        new("978-0-553-38380-6", "The House of the Spirits", "Isabel Allende", Genre.Fiction,
            PlanTier.Basic, 1800,
            [new("New York", "Midtown", 2), new("New York", "Harlem", 1), new("Chicago", "Loop", 1)]),

        new("978-968-16-7515-3", "Pedro Paramo", "Juan Rulfo", Genre.Fiction,
            PlanTier.Basic, 1400,
            [new("Chicago", "Loop", 2), new("Austin", "Mueller", 1)]),

        new("978-0-679-75255-4", "Discipline and Punish", "Michel Foucault", Genre.Essay,
            PlanTier.Max, 2200,
            [new("New York", "Midtown", 1)]),

        new("978-1-4736-9855-5", "Papyrus", "Irene Vallejo", Genre.Essay,
            PlanTier.Plus, 2600,
            [new("Austin", "Mueller", 3), new("New York", "Midtown", 2), new("Chicago", "Pilsen", 1)]),

        new("978-1-5290-1150-3", "Klara and the Sun", "Kazuo Ishiguro", Genre.ScienceFiction,
            PlanTier.Plus, 1900,
            [new("New York", "Harlem", 2), new("New York", "Midtown", 1)]),

        // Deliberately empty: the only book that makes "All copies out" observable. Its single
        // branch owns three volumes and has none free, which is the prototype's "0 / 3".
        new("978-0-312-42748-0", "The Savage Detectives", "Roberto Bolano", Genre.Fiction,
            PlanTier.Plus, 2400,
            [new("Chicago", "Loop", Available: 0, Total: 3)]),

        new("978-0-06-231609-7", "Sapiens", "Yuval Noah Harari", Genre.History,
            PlanTier.Basic, 2300,
            [new("Austin", "Mueller", 3), new("New York", "Midtown", 2)]),

        new("978-0-06-088328-7", "One Hundred Years of Solitude", "Gabriel Garcia Marquez", Genre.Fiction,
            PlanTier.Basic, 2000,
            [new("New York", "Midtown", 4), new("Chicago", "Loop", 2), new("Austin", "Mueller", 1)]),

        new("978-0-231-16947-1", "Oblivion: A Memoir", "Hector Abad Faciolince", Genre.Biography,
            PlanTier.Plus, 1600,
            [new("Chicago", "Pilsen", 2)]),

        new("978-0-374-52748-1", "The Time of the Hero", "Mario Vargas Llosa", Genre.Fiction,
            PlanTier.Plus, 1700,
            [new("Austin", "Mueller", 3)]),

        new("978-1-4493-7332-0", "Designing Data-Intensive Applications", "Martin Kleppmann", Genre.Technical,
            PlanTier.Max, 4500,
            [new("New York", "Harlem", 2)]),

        new("978-1-4000-7927-8", "Kafka on the Shore", "Haruki Murakami", Genre.Fiction,
            PlanTier.Plus, 2100,
            [new("New York", "Midtown", 2), new("Chicago", "Loop", 1)])
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        var existingIsbns = (await context.Books
                .Select(book => book.Isbn.Value)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var libraries = await context.Libraries
            .Join(context.Cities, library => library.CityId, city => city.Id,
                (library, city) => new { library.Id, library.Name, CityName = city.Name })
            .ToListAsync(cancellationToken);

        var seeded = 0;

        foreach (var seed in Books)
        {
            var isbn = Isbn.Create(seed.Isbn);

            if (isbn.IsFailure)
            {
                throw new InvalidOperationException($"Seed ISBN '{seed.Isbn}' is invalid.");
            }

            if (existingIsbns.Contains(isbn.Value.Value))
            {
                continue;
            }

            var book = Book.CreateDraft(
                isbn.Value, seed.Title, seed.Author, publisher: null, seed.Genre, seed.Tier,
                Money.FromCents(seed.PriceCents), coverUrl: null, now);

            if (book.IsFailure)
            {
                throw new InvalidOperationException($"Seed book '{seed.Title}' is invalid: {book.Error.Message}");
            }

            foreach (var stock in seed.Stock)
            {
                var library = libraries.FirstOrDefault(
                    l => l.Name == stock.Branch && l.CityName == stock.City)
                    ?? throw new InvalidOperationException(
                        $"Seed book '{seed.Title}' needs library '{stock.City} — {stock.Branch}', "
                        + "which the network seed did not create.");

                // A branch with nothing free still gets a holding: the row is what tells the
                // interface the branch stocks the title at all, which is what "0 / 3" means. The
                // volumes are added and then taken off the shelf, so both counts end up right.
                var total = stock.Total ?? stock.Available;

                book.Value.AddCopies(library.Id, total);

                for (var taken = 0; taken < total - stock.Available; taken++)
                {
                    book.Value.CopyAt(library.Id)!.Take();
                }
            }

            book.Value.Publish(now);

            // The events exist to trigger audit entries for a real staff action. Seeding is not one,
            // so they are cleared rather than dispatched.
            book.Value.ClearDomainEvents();

            context.Books.Add(book.Value);
            seeded++;
        }

        if (seeded == 0)
        {
            logger.LogInformation("Catalogue seed is already complete. Nothing inserted.");
            return;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Seeded {Count} book(s) into the catalogue.", seeded);
    }
}
