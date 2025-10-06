using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveNodeIdColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_research_nodes_node_id",
                table: "research_nodes");

            migrationBuilder.DropColumn(
                name: "node_id",
                table: "research_nodes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "node_id",
                table: "research_nodes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_research_nodes_node_id",
                table: "research_nodes",
                column: "node_id",
                unique: true);
        }
    }
}
