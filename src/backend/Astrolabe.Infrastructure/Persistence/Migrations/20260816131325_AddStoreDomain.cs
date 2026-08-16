using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "orders",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    fulfilment = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    points_earned = table.Column<int>(type: "integer", nullable: false),
                    placed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    discount_total_cents = table.Column<long>(type: "bigint", nullable: false),
                    shipping_fee_cents = table.Column<long>(type: "bigint", nullable: false),
                    subtotal_cents = table.Column<long>(type: "bigint", nullable: false),
                    total_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => x.id);
                    table.ForeignKey(
                        name: "fk_orders_users_member_id",
                        column: x => x.member_id,
                        principalSchema: "astrolabe",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "points_movements",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    point_cents = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_points_movements", x => x.id);
                    table.ForeignKey(
                        name: "fk_points_movements_users_member_id",
                        column: x => x.member_id,
                        principalSchema: "astrolabe",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    discount_percent = table.Column<int>(type: "integer", nullable: false),
                    discount_amount_cents = table.Column<long>(type: "bigint", nullable: false),
                    line_total_cents = table.Column<long>(type: "bigint", nullable: false),
                    unit_price_cents = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_lines_books_book_id",
                        column: x => x.book_id,
                        principalSchema: "astrolabe",
                        principalTable: "books",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_order_lines_orders_order_id",
                        column: x => x.order_id,
                        principalSchema: "astrolabe",
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_book_id",
                schema: "astrolabe",
                table: "order_lines",
                column: "book_id");

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_order_id",
                schema: "astrolabe",
                table: "order_lines",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_orders_member_id_idempotency_key",
                schema: "astrolabe",
                table: "orders",
                columns: new[] { "member_id", "idempotency_key" },
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_orders_member_id_placed_at",
                schema: "astrolabe",
                table: "orders",
                columns: new[] { "member_id", "placed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_points_movements_member_id_occurred_at",
                schema: "astrolabe",
                table: "points_movements",
                columns: new[] { "member_id", "occurred_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_lines",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "points_movements",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "astrolabe");
        }
    }
}
