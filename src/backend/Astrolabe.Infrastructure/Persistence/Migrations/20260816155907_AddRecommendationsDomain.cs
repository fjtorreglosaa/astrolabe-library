using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationsDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "library_ai_configurations",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    credential_cipher = table.Column<byte[]>(type: "bytea", nullable: false),
                    credential_key_version = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    last_failure_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_library_ai_configurations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_sets",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    member_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    generated_by_library_id = table.Column<Guid>(type: "uuid", nullable: true),
                    generated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recommendation_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "recommendation_items",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    book_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reason = table.Column<string>(type: "character varying(280)", maxLength: 280, nullable: false),
                    match_percent = table.Column<int>(type: "integer", nullable: false),
                    recommendation_set_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_recommendation_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_recommendation_items_recommendation_sets_recommendation_set",
                        column: x => x.recommendation_set_id,
                        principalSchema: "astrolabe",
                        principalTable: "recommendation_sets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_library_ai_configurations_library_id",
                schema: "astrolabe",
                table: "library_ai_configurations",
                column: "library_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_recommendation_items_recommendation_set_id",
                schema: "astrolabe",
                table: "recommendation_items",
                column: "recommendation_set_id");

            migrationBuilder.CreateIndex(
                name: "ix_recommendation_sets_generated_by_library_id",
                schema: "astrolabe",
                table: "recommendation_sets",
                column: "generated_by_library_id");

            migrationBuilder.CreateIndex(
                name: "ix_recommendation_sets_member_id",
                schema: "astrolabe",
                table: "recommendation_sets",
                column: "member_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "library_ai_configurations",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "recommendation_items",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "recommendation_sets",
                schema: "astrolabe");
        }
    }
}
