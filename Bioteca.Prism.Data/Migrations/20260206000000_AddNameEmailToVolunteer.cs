using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNameEmailToVolunteer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                table: "volunteer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "volunteer",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_volunteer_email",
                table: "volunteer",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_volunteer_email",
                table: "volunteer");

            migrationBuilder.DropColumn(
                name: "email",
                table: "volunteer");

            migrationBuilder.DropColumn(
                name: "name",
                table: "volunteer");
        }
    }
}
