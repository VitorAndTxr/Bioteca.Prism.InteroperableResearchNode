using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <summary>
    /// Migrates Application-Research relationship from many-to-many (via research_application join table)
    /// to one-to-many (direct research_id FK on application table).
    ///
    /// The entity model was refactored but no migration was generated, causing
    /// "42703: column a.research_id does not exist" when EF Core queries application with Include.
    /// </summary>
    public partial class MigrateApplicationDirectFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Step 1: Add research_id column to application table (nullable initially to allow data migration)
            migrationBuilder.AddColumn<Guid>(
                name: "research_id",
                table: "application",
                type: "uuid",
                nullable: true);

            // Step 2: Populate research_id from the research_application join table.
            // If an application has multiple research associations, takes the most recent one.
            migrationBuilder.Sql(@"
                UPDATE application
                SET research_id = sub.research_id
                FROM (
                    SELECT DISTINCT ON (application_id) application_id, research_id
                    FROM research_application
                    ORDER BY application_id, added_at DESC
                ) sub
                WHERE application.application_id = sub.application_id
            ");

            // Step 3: Delete orphaned applications that have no research association
            migrationBuilder.Sql(@"
                DELETE FROM application
                WHERE research_id IS NULL
            ");

            // Step 4: Make research_id NOT NULL
            migrationBuilder.AlterColumn<Guid>(
                name: "research_id",
                table: "application",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            // Step 5: Add index on research_id
            migrationBuilder.CreateIndex(
                name: "IX_application_ResearchId",
                table: "application",
                column: "research_id");

            // Step 6: Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_application_research_research_id",
                table: "application",
                column: "research_id",
                principalTable: "research",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Step 7: Drop the old research_application join table
            migrationBuilder.DropTable(
                name: "research_application");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recreate the research_application join table
            migrationBuilder.CreateTable(
                name: "research_application",
                columns: table => new
                {
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, defaultValue: "default"),
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

            // Populate join table from direct FK
            migrationBuilder.Sql(@"
                INSERT INTO research_application (research_id, application_id, role, added_at)
                SELECT research_id, application_id, 'default', NOW()
                FROM application
                WHERE research_id IS NOT NULL
            ");

            migrationBuilder.CreateIndex(
                name: "ix_research_application_application_id",
                table: "research_application",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_application_research_id",
                table: "research_application",
                column: "research_id");

            // Remove direct FK from application table
            migrationBuilder.DropForeignKey(
                name: "FK_application_research_research_id",
                table: "application");

            migrationBuilder.DropIndex(
                name: "IX_application_ResearchId",
                table: "application");

            migrationBuilder.DropColumn(
                name: "research_id",
                table: "application");
        }
    }
}
