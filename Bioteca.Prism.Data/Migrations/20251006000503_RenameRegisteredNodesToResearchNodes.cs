using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameRegisteredNodesToResearchNodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename table instead of dropping and recreating to preserve data
            migrationBuilder.RenameTable(
                name: "registered_nodes",
                newName: "research_nodes");

            // Rename primary key constraint
            migrationBuilder.RenameIndex(
                name: "PK_registered_nodes",
                table: "research_nodes",
                newName: "PK_research_nodes");

            // Rename indexes
            migrationBuilder.RenameIndex(
                name: "ix_registered_nodes_access_level",
                table: "research_nodes",
                newName: "ix_research_nodes_access_level");

            migrationBuilder.RenameIndex(
                name: "ix_registered_nodes_certificate_fingerprint",
                table: "research_nodes",
                newName: "ix_research_nodes_certificate_fingerprint");

            migrationBuilder.RenameIndex(
                name: "ix_registered_nodes_last_authenticated_at",
                table: "research_nodes",
                newName: "ix_research_nodes_last_authenticated_at");

            migrationBuilder.RenameIndex(
                name: "ix_registered_nodes_registered_at",
                table: "research_nodes",
                newName: "ix_research_nodes_registered_at");

            migrationBuilder.RenameIndex(
                name: "ix_registered_nodes_status",
                table: "research_nodes",
                newName: "ix_research_nodes_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: Rename table back to original name
            migrationBuilder.RenameTable(
                name: "research_nodes",
                newName: "registered_nodes");

            // Rename primary key constraint back
            migrationBuilder.RenameIndex(
                name: "PK_research_nodes",
                table: "registered_nodes",
                newName: "PK_registered_nodes");

            // Rename indexes back
            migrationBuilder.RenameIndex(
                name: "ix_research_nodes_access_level",
                table: "registered_nodes",
                newName: "ix_registered_nodes_access_level");

            migrationBuilder.RenameIndex(
                name: "ix_research_nodes_certificate_fingerprint",
                table: "registered_nodes",
                newName: "ix_registered_nodes_certificate_fingerprint");

            migrationBuilder.RenameIndex(
                name: "ix_research_nodes_last_authenticated_at",
                table: "registered_nodes",
                newName: "ix_registered_nodes_last_authenticated_at");

            migrationBuilder.RenameIndex(
                name: "ix_research_nodes_registered_at",
                table: "registered_nodes",
                newName: "ix_registered_nodes_registered_at");

            migrationBuilder.RenameIndex(
                name: "ix_research_nodes_status",
                table: "registered_nodes",
                newName: "ix_registered_nodes_status");
        }
    }
}
