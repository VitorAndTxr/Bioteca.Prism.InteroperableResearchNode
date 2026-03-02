using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveMedicalHistoryColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_record_session_target_area_target_area_id",
                table: "record_session");

            migrationBuilder.DropIndex(
                name: "ix_target_area_record_session_id",
                table: "target_area");

            migrationBuilder.DropIndex(
                name: "ix_record_session_target_area_id",
                table: "record_session");

            migrationBuilder.DropColumn(
                name: "medical_history",
                table: "volunteer");

            migrationBuilder.CreateIndex(
                name: "ix_target_area_record_session_id",
                table: "target_area",
                column: "record_session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_record_session_target_area_id",
                table: "record_session",
                column: "target_area_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_target_area_record_session_id",
                table: "target_area");

            migrationBuilder.DropIndex(
                name: "ix_record_session_target_area_id",
                table: "record_session");

            migrationBuilder.AddColumn<string>(
                name: "medical_history",
                table: "volunteer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_target_area_record_session_id",
                table: "target_area",
                column: "record_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_session_target_area_id",
                table: "record_session",
                column: "target_area_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_record_session_target_area_target_area_id",
                table: "record_session",
                column: "target_area_id",
                principalTable: "target_area",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
