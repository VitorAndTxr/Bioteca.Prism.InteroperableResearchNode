using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class TransformApplicationDeviceToManyToMany : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_application_research_research_id",
                table: "application");

            migrationBuilder.DropForeignKey(
                name: "FK_device_research_research_id",
                table: "device");

            migrationBuilder.DropIndex(
                name: "ix_device_research_id",
                table: "device");

            migrationBuilder.DropIndex(
                name: "ix_application_research_id",
                table: "application");

            migrationBuilder.DropColumn(
                name: "research_id",
                table: "device");

            migrationBuilder.DropColumn(
                name: "research_id",
                table: "application");

            migrationBuilder.CreateTable(
                name: "research_application",
                columns: table => new
                {
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    configuration = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_application", x => new { x.research_id, x.application_id });
                    table.ForeignKey(
                        name: "FK_research_application_application_application_id",
                        column: x => x.application_id,
                        principalTable: "application",
                        principalColumn: "application_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_research_application_research_research_id",
                        column: x => x.research_id,
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "research_device",
                columns: table => new
                {
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    calibration_status = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    last_calibration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_device", x => new { x.research_id, x.device_id });
                    table.ForeignKey(
                        name: "FK_research_device_device_device_id",
                        column: x => x.device_id,
                        principalTable: "device",
                        principalColumn: "device_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_research_device_research_research_id",
                        column: x => x.research_id,
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_research_application_application_id",
                table: "research_application",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_application_research_id",
                table: "research_application",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_device_device_id",
                table: "research_device",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_device_research_id",
                table: "research_device",
                column: "research_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "research_application");

            migrationBuilder.DropTable(
                name: "research_device");

            migrationBuilder.AddColumn<Guid>(
                name: "research_id",
                table: "device",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "research_id",
                table: "application",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "ix_device_research_id",
                table: "device",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_application_research_id",
                table: "application",
                column: "research_id");

            migrationBuilder.AddForeignKey(
                name: "FK_application_research_research_id",
                table: "application",
                column: "research_id",
                principalTable: "research",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_device_research_research_id",
                table: "device",
                column: "research_id",
                principalTable: "research",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
