using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Deliberately empty.
    ///
    /// <para>
    /// Isbn and StarRating moved from value converters to owned types. Both mappings produce the
    /// same columns, so there is no schema change — but the model snapshot did change, and EF needs
    /// a migration to record it. Deleting this file would leave the snapshot disagreeing with the
    /// migration history and make the next migration try to recreate the catalogue.
    /// </para>
    /// <para>
    /// The change itself was a defect fix, not a preference: a value converter makes the wrapped
    /// member invisible to the provider, so every query touching <c>Isbn.Value</c> or
    /// <c>Rating.Stars</c> — catalogue search and the rating average among them — failed at run time.
    /// </para>
    /// </summary>
    public partial class MapCatalogValueObjectsAsOwned : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
