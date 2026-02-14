using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionAnnotationAndNullableFKs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_record_channel_sensor_sensor_id",
                table: "record_channel");

            migrationBuilder.DropForeignKey(
                name: "FK_record_session_research_research_id",
                table: "record_session");

            migrationBuilder.AlterColumn<Guid>(
                name: "research_id",
                table: "record_session",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<Guid>(
                name: "sensor_id",
                table: "record_channel",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.CreateTable(
                name: "session_annotation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_session_annotation", x => x.id);
                    table.ForeignKey(
                        name: "FK_session_annotation_record_session_record_session_id",
                        column: x => x.record_session_id,
                        principalTable: "record_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_session_annotation_created_at",
                table: "session_annotation",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_session_annotation_record_session_id",
                table: "session_annotation",
                column: "record_session_id");

            migrationBuilder.AddForeignKey(
                name: "FK_record_channel_sensor_sensor_id",
                table: "record_channel",
                column: "sensor_id",
                principalTable: "sensor",
                principalColumn: "sensor_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_record_session_research_research_id",
                table: "record_session",
                column: "research_id",
                principalTable: "research",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_record_channel_sensor_sensor_id",
                table: "record_channel");

            migrationBuilder.DropForeignKey(
                name: "FK_record_session_research_research_id",
                table: "record_session");

            migrationBuilder.DropTable(
                name: "session_annotation");

            migrationBuilder.AlterColumn<Guid>(
                name: "research_id",
                table: "record_session",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "sensor_id",
                table: "record_channel",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_record_channel_sensor_sensor_id",
                table: "record_channel",
                column: "sensor_id",
                principalTable: "sensor",
                principalColumn: "sensor_id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_record_session_research_research_id",
                table: "record_session",
                column: "research_id",
                principalTable: "research",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
