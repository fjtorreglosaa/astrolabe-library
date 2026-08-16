using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// STR-017. Records how many reward point-cents were put toward each order (BR-STR-007).
    ///
    /// <para>
    /// The default of zero is exactly right for every order already placed: redemption did not
    /// exist, so none of them used any. No backfill is needed and none would be honest.
    /// </para>
    /// <para>
    /// No column for what the card was charged. That is the total less this, both of them frozen,
    /// so storing it would be a third figure obliged to agree with the other two.
    /// </para>
    /// </summary>
    public partial class AddRewardPointRedemption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "points_redeemed",
                schema: "astrolabe",
                table: "orders",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "points_redeemed",
                schema: "astrolabe",
                table: "orders");
        }
    }
}
