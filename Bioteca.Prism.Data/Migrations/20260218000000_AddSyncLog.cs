using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSyncLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add UpdatedAt to record_channel (was missing, required for incremental sync)
            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "record_channel",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Add CreatedAt + UpdatedAt to snomed_laterality (were missing, required for incremental sync)
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "snomed_laterality",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "snomed_laterality",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Add CreatedAt + UpdatedAt to snomed_topographical_modifier (were missing, required for incremental sync)
            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "snomed_topographical_modifier",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "snomed_topographical_modifier",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            // Create sync_logs table
            migrationBuilder.CreateTable(
                name: "sync_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    remote_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    last_synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    entities_received = table.Column<string>(type: "jsonb", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sync_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_sync_logs_research_nodes_remote_node_id",
                        column: x => x.remote_node_id,
                        principalTable: "research_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Indexes on sync_logs
            migrationBuilder.CreateIndex(
                name: "ix_sync_logs_remote_node_id",
                table: "sync_logs",
                column: "remote_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_sync_logs_status",
                table: "sync_logs",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sync_logs_started_at",
                table: "sync_logs",
                column: "started_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "sync_logs");

            migrationBuilder.DropColumn(name: "updated_at", table: "record_channel");

            migrationBuilder.DropColumn(name: "created_at", table: "snomed_laterality");
            migrationBuilder.DropColumn(name: "updated_at", table: "snomed_laterality");

            migrationBuilder.DropColumn(name: "created_at", table: "snomed_topographical_modifier");
            migrationBuilder.DropColumn(name: "updated_at", table: "snomed_topographical_modifier");
        }
    }
}
