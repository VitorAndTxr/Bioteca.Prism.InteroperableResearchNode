using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveClinicalConditionUnusedColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_volunteer_clinical_conditions_snomed_severity_codes_severit~",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropIndex(
                name: "ix_volunteer_clinical_conditions_severity_code",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropColumn(
                name: "abatement_date",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropColumn(
                name: "clinical_notes",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropColumn(
                name: "onset_date",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropColumn(
                name: "severity_code",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropColumn(
                name: "verification_status",
                table: "volunteer_clinical_conditions");

            migrationBuilder.AddColumn<string>(
                name: "SnomedSeverityCodeCode",
                table: "volunteer_clinical_conditions",
                type: "character varying(50)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_volunteer_clinical_conditions_SnomedSeverityCodeCode",
                table: "volunteer_clinical_conditions",
                column: "SnomedSeverityCodeCode");

            migrationBuilder.AddForeignKey(
                name: "FK_volunteer_clinical_conditions_snomed_severity_codes_SnomedS~",
                table: "volunteer_clinical_conditions",
                column: "SnomedSeverityCodeCode",
                principalTable: "snomed_severity_codes",
                principalColumn: "code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_volunteer_clinical_conditions_snomed_severity_codes_SnomedS~",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropIndex(
                name: "IX_volunteer_clinical_conditions_SnomedSeverityCodeCode",
                table: "volunteer_clinical_conditions");

            migrationBuilder.DropColumn(
                name: "SnomedSeverityCodeCode",
                table: "volunteer_clinical_conditions");

            migrationBuilder.AddColumn<DateTime>(
                name: "abatement_date",
                table: "volunteer_clinical_conditions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "clinical_notes",
                table: "volunteer_clinical_conditions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "onset_date",
                table: "volunteer_clinical_conditions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "severity_code",
                table: "volunteer_clinical_conditions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "verification_status",
                table: "volunteer_clinical_conditions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "ix_volunteer_clinical_conditions_severity_code",
                table: "volunteer_clinical_conditions",
                column: "severity_code");

            migrationBuilder.AddForeignKey(
                name: "FK_volunteer_clinical_conditions_snomed_severity_codes_severit~",
                table: "volunteer_clinical_conditions",
                column: "severity_code",
                principalTable: "snomed_severity_codes",
                principalColumn: "code",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
