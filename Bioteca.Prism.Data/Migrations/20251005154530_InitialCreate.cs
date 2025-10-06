using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registered_nodes",
                columns: table => new
                {
                    node_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    node_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    certificate = table.Column<string>(type: "text", nullable: false),
                    certificate_fingerprint = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    node_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    node_access_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    contact_info = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    institution_details = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_authenticated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registered_nodes", x => x.node_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_registered_nodes_access_level",
                table: "registered_nodes",
                column: "node_access_level");

            migrationBuilder.CreateIndex(
                name: "ix_registered_nodes_certificate_fingerprint",
                table: "registered_nodes",
                column: "certificate_fingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registered_nodes_last_authenticated_at",
                table: "registered_nodes",
                column: "last_authenticated_at");

            migrationBuilder.CreateIndex(
                name: "ix_registered_nodes_registered_at",
                table: "registered_nodes",
                column: "registered_at");

            migrationBuilder.CreateIndex(
                name: "ix_registered_nodes_status",
                table: "registered_nodes",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registered_nodes");
        }
    }
}
