using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddGuidIdToResearchNodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_research_nodes",
                table: "research_nodes");

            // Add id column with temporary default
            migrationBuilder.AddColumn<Guid>(
                name: "id",
                table: "research_nodes",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()");

            // Generate GUIDs for existing rows (if any)
            migrationBuilder.Sql(
                @"UPDATE research_nodes SET id = gen_random_uuid() WHERE id = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.AddPrimaryKey(
                name: "PK_research_nodes",
                table: "research_nodes",
                column: "id");

            migrationBuilder.CreateIndex(
                name: "ix_research_nodes_node_id",
                table: "research_nodes",
                column: "node_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_research_nodes",
                table: "research_nodes");

            migrationBuilder.DropIndex(
                name: "ix_research_nodes_node_id",
                table: "research_nodes");

            migrationBuilder.DropColumn(
                name: "id",
                table: "research_nodes");

            migrationBuilder.AddPrimaryKey(
                name: "PK_research_nodes",
                table: "research_nodes",
                column: "node_id");
        }
    }
}
