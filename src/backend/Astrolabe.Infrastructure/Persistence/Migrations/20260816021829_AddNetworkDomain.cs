using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Astrolabe.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNetworkDomain : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "astrolabe");

            migrationBuilder.CreateTable(
                name: "admin_invitations",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<int>(type: "integer", nullable: false),
                    library_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false),
                    token_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_admin_invitations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    iso_code = table.Column<string>(type: "character(2)", fixedLength: true, maxLength: 2, nullable: false),
                    is_hidden_from_registration = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cities",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    home_library_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cities", x => x.id);
                    table.ForeignKey(
                        name: "fk_cities_countries_country_id",
                        column: x => x.country_id,
                        principalSchema: "astrolabe",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "libraries",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    city_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_libraries", x => x.id);
                    table.ForeignKey(
                        name: "fk_libraries_cities_city_id",
                        column: x => x.city_id,
                        principalSchema: "astrolabe",
                        principalTable: "cities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "library_assignments",
                schema: "astrolabe",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    library_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revoked_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_library_assignments", x => x.id);
                    table.ForeignKey(
                        name: "fk_library_assignments_libraries_library_id",
                        column: x => x.library_id,
                        principalSchema: "astrolabe",
                        principalTable: "libraries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_admin_invitations_token_hash",
                schema: "astrolabe",
                table: "admin_invitations",
                column: "token_hash");

            migrationBuilder.CreateIndex(
                name: "ix_admin_invitations_user_id",
                schema: "astrolabe",
                table: "admin_invitations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_cities_country_id_name",
                schema: "astrolabe",
                table: "cities",
                columns: new[] { "country_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_countries_iso_code",
                schema: "astrolabe",
                table: "countries",
                column: "iso_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_libraries_city_id_name",
                schema: "astrolabe",
                table: "libraries",
                columns: new[] { "city_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_libraries_is_active",
                schema: "astrolabe",
                table: "libraries",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_library_assignments_library_id",
                schema: "astrolabe",
                table: "library_assignments",
                column: "library_id");

            migrationBuilder.CreateIndex(
                name: "ix_library_assignments_user_id_library_id",
                schema: "astrolabe",
                table: "library_assignments",
                columns: new[] { "user_id", "library_id" },
                unique: true,
                filter: "revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_library_assignments_user_id_revoked_at",
                schema: "astrolabe",
                table: "library_assignments",
                columns: new[] { "user_id", "revoked_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_invitations",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "library_assignments",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "libraries",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "cities",
                schema: "astrolabe");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "astrolabe");
        }
    }
}
