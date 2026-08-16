using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Deliberately empty, for the same reason as <c>MapCatalogValueObjectsAsOwned</c>.
    ///
    /// <para>
    /// <c>Money</c> moved from a value converter to a complex type. The column is unchanged —
    /// <c>retail_price_cents</c>, still a bigint — but the model snapshot changed, and EF needs a
    /// migration to record it. Deleting this file would leave the snapshot disagreeing with the
    /// migration history.
    /// </para>
    /// <para>
    /// The change was a defect fix. A converter hides <c>Money.Cents</c> from the provider, so
    /// ordering the catalogue by price threw at run time while every unit test passed. Money is
    /// filtered and sorted on in <c>store</c> and <c>fines</c> too, so fixing it once here is
    /// cheaper than discovering it three more times.
    /// </para>
    /// </summary>
    public partial class MapMoneyAsComplexType : Migration
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
