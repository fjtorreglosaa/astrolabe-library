using Astrolabe.Domain.Features.Network.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Astrolabe.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the library network. Idempotent: it inserts only what is missing, so it can run on every
/// startup without duplicating rows or resetting anything an administrator has changed.
///
/// The United States data is taken verbatim from the approved prototype and is authoritative.
/// The remaining fifteen cities are seed data introduced when NET-OPEN-001 was resolved in favour of
/// covering all six registration countries. They are product data and may be renamed freely.
///
/// Identifiers are deterministic, derived from the entity's natural key, so a re-run finds the same
/// rows and other seeders can reference a library without querying for it first.
/// </summary>
public sealed class NetworkSeeder(AstrolabeDbContext context, ILogger<NetworkSeeder> logger)
{
    /// <summary>The home library of each city is listed first.</summary>
    private static readonly (string Country, string Iso, (string City, string[] Libraries)[] Cities)[] Network =
    [
        ("United States", "US",
        [
            ("New York", ["Midtown", "Harlem"]),
            ("Chicago", ["Loop", "Pilsen"]),
            ("Austin", ["Mueller"])
        ]),
        ("Canada", "CA",
        [
            ("Toronto", ["Annex", "Leslieville"]),
            ("Vancouver", ["Kitsilano", "Gastown"]),
            ("Montreal", ["Plateau", "Verdun"])
        ]),
        ("United Kingdom", "GB",
        [
            ("London", ["Bloomsbury", "Shoreditch"]),
            ("Manchester", ["Ancoats", "Didsbury"]),
            ("Edinburgh", ["Newington", "Leith"])
        ]),
        ("Mexico", "MX",
        [
            ("Mexico City", ["Condesa", "Coyoacan"]),
            ("Guadalajara", ["Chapalita", "Americana"]),
            ("Monterrey", ["San Pedro", "Obispado"])
        ]),
        ("Colombia", "CO",
        [
            ("Bogota", ["Chapinero", "Usaquen"]),
            ("Medellin", ["Laureles", "El Poblado"]),
            ("Cali", ["Granada", "San Antonio"])
        ]),
        ("Spain", "ES",
        [
            ("Madrid", ["Chamberi", "Lavapies"]),
            ("Barcelona", ["Gracia", "Eixample"]),
            ("Valencia", ["Ruzafa", "El Carmen"])
        ])
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var existingCountries = await context.Countries
            .Select(c => c.IsoCode)
            .ToListAsync(cancellationToken);

        var existingCities = await context.Cities
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        var existingLibraries = await context.Libraries
            .Select(l => l.Id)
            .ToListAsync(cancellationToken);

        var countriesAdded = 0;
        var citiesAdded = 0;
        var librariesAdded = 0;

        foreach (var (countryName, iso, cities) in Network)
        {
            var countryId = DeterministicId($"country:{iso}");

            if (!existingCountries.Contains(iso))
            {
                var country = Country.Create(countryId, countryName, iso);

                if (country.IsFailure)
                {
                    // Seed data is authored here, so a failure is a programming error, not a
                    // runtime condition worth degrading over.
                    throw new InvalidOperationException(
                        $"Seed country '{countryName}' is invalid: {country.Error.Message}");
                }

                context.Countries.Add(country.Value);
                countriesAdded++;
            }

            foreach (var (cityName, libraryNames) in cities)
            {
                var cityId = DeterministicId($"city:{iso}:{cityName}");
                City? city = null;

                if (!existingCities.Contains(cityId))
                {
                    var created = City.Create(cityId, countryId, cityName);

                    if (created.IsFailure)
                    {
                        throw new InvalidOperationException(
                            $"Seed city '{cityName}' is invalid: {created.Error.Message}");
                    }

                    city = created.Value;
                    context.Cities.Add(city);
                    citiesAdded++;
                }

                Library? homeLibrary = null;

                foreach (var libraryName in libraryNames)
                {
                    var libraryId = DeterministicId($"library:{iso}:{cityName}:{libraryName}");

                    if (existingLibraries.Contains(libraryId))
                    {
                        continue;
                    }

                    var created = Library.Create(libraryId, cityId, libraryName);

                    if (created.IsFailure)
                    {
                        throw new InvalidOperationException(
                            $"Seed library '{libraryName}' is invalid: {created.Error.Message}");
                    }

                    context.Libraries.Add(created.Value);
                    librariesAdded++;

                    // BR-NET-003: the first library listed for a city is its home library.
                    homeLibrary ??= created.Value;
                }

                if (city is not null && homeLibrary is not null)
                {
                    var designated = city.DesignateHomeLibrary(homeLibrary);

                    if (designated.IsFailure)
                    {
                        throw new InvalidOperationException(
                            $"Seed home library for '{cityName}' is invalid: {designated.Error.Message}");
                    }
                }
            }
        }

        if (countriesAdded + citiesAdded + librariesAdded == 0)
        {
            logger.LogInformation("Network seed is already complete. Nothing inserted.");
            return;
        }

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Network seeded: {Countries} countries, {Cities} cities, {Libraries} libraries inserted.",
            countriesAdded, citiesAdded, librariesAdded);
    }

    /// <summary>
    /// Derives a stable identifier from a natural key, so re-running the seeder finds the same rows
    /// and other seeders can reference a library without a lookup. Not a security primitive: it is
    /// a naming scheme, and the input is fixed seed text.
    /// </summary>
    private static Guid DeterministicId(string naturalKey)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(naturalKey));

        return new Guid(hash.AsSpan(0, 16));
    }
}
