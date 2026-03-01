using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class EntityMappingCorrections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_target_area_record_channel_record_channel_id",
                table: "target_area");

            migrationBuilder.DropForeignKey(
                name: "FK_target_area_snomed_topographical_modifier_topographical_mod~",
                table: "target_area");

            migrationBuilder.DropIndex(
                name: "IX_target_area_topographical_modifier_code",
                table: "target_area");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "target_area");

            migrationBuilder.DropColumn(
                name: "topographical_modifier_code",
                table: "target_area");

            migrationBuilder.DropColumn(
                name: "clinical_context",
                table: "record_session");

            migrationBuilder.DropColumn(
                name: "annotations",
                table: "record_channel");

            migrationBuilder.DropColumn(
                name: "notes",
                table: "record");

            migrationBuilder.RenameColumn(
                name: "record_channel_id",
                table: "target_area",
                newName: "record_session_id");

            migrationBuilder.RenameIndex(
                name: "ix_target_area_record_channel_id",
                table: "target_area",
                newName: "ix_target_area_record_session_id");

            migrationBuilder.AddColumn<Guid>(
                name: "target_area_id",
                table: "record_session",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "target_area_topographical_modifier",
                columns: table => new
                {
                    target_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    topographical_modifier_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_area_topographical_modifier", x => new { x.target_area_id, x.topographical_modifier_code });
                    table.ForeignKey(
                        name: "FK_target_area_topographical_modifier_snomed_topographical_mod~",
                        column: x => x.topographical_modifier_code,
                        principalTable: "snomed_topographical_modifier",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_target_area_topographical_modifier_target_area_target_area_~",
                        column: x => x.target_area_id,
                        principalTable: "target_area",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_record_session_target_area_id",
                table: "record_session",
                column: "target_area_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_target_area_topographical_modifier_topographical_modifier_c~",
                table: "target_area_topographical_modifier",
                column: "topographical_modifier_code");

            migrationBuilder.AddForeignKey(
                name: "FK_record_session_target_area_target_area_id",
                table: "record_session",
                column: "target_area_id",
                principalTable: "target_area",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_target_area_record_session_record_session_id",
                table: "target_area",
                column: "record_session_id",
                principalTable: "record_session",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_record_session_target_area_target_area_id",
                table: "record_session");

            migrationBuilder.DropForeignKey(
                name: "FK_target_area_record_session_record_session_id",
                table: "target_area");

            migrationBuilder.DropTable(
                name: "target_area_topographical_modifier");

            migrationBuilder.DropIndex(
                name: "ix_record_session_target_area_id",
                table: "record_session");

            migrationBuilder.DropColumn(
                name: "target_area_id",
                table: "record_session");

            migrationBuilder.RenameColumn(
                name: "record_session_id",
                table: "target_area",
                newName: "record_channel_id");

            migrationBuilder.RenameIndex(
                name: "ix_target_area_record_session_id",
                table: "target_area",
                newName: "ix_target_area_record_channel_id");

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "target_area",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "topographical_modifier_code",
                table: "target_area",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "clinical_context",
                table: "record_session",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<JsonDocument>(
                name: "annotations",
                table: "record_channel",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "notes",
                table: "record",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_target_area_topographical_modifier_code",
                table: "target_area",
                column: "topographical_modifier_code");

            migrationBuilder.AddForeignKey(
                name: "FK_target_area_record_channel_record_channel_id",
                table: "target_area",
                column: "record_channel_id",
                principalTable: "record_channel",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_target_area_snomed_topographical_modifier_topographical_mod~",
                table: "target_area",
                column: "topographical_modifier_code",
                principalTable: "snomed_topographical_modifier",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
