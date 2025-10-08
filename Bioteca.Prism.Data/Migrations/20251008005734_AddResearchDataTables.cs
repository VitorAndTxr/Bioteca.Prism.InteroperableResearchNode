using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bioteca.Prism.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddResearchDataTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "research",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research", x => x.id);
                    table.ForeignKey(
                        name: "FK_research_research_nodes_research_node_id",
                        column: x => x.research_node_id,
                        principalTable: "research_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "researcher",
                columns: table => new
                {
                    researcher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    institution = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    role = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_researcher", x => x.researcher_id);
                    table.ForeignKey(
                        name: "FK_researcher_research_nodes_research_node_id",
                        column: x => x.research_node_id,
                        principalTable: "research_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "snomed_body_region",
                columns: table => new
                {
                    snomed_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    parent_region_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snomed_body_region", x => x.snomed_code);
                    table.ForeignKey(
                        name: "FK_snomed_body_region_snomed_body_region_parent_region_code",
                        column: x => x.parent_region_code,
                        principalTable: "snomed_body_region",
                        principalColumn: "snomed_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "snomed_laterality",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snomed_laterality", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "snomed_topographical_modifier",
                columns: table => new
                {
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snomed_topographical_modifier", x => x.code);
                });

            migrationBuilder.CreateTable(
                name: "volunteer",
                columns: table => new
                {
                    volunteer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_node_id = table.Column<Guid>(type: "uuid", nullable: false),
                    volunteer_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    blood_type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    height = table.Column<float>(type: "real", nullable: true),
                    weight = table.Column<float>(type: "real", nullable: true),
                    medical_history = table.Column<string>(type: "text", nullable: false),
                    consent_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    enrolled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_volunteer", x => x.volunteer_id);
                    table.ForeignKey(
                        name: "FK_volunteer_research_nodes_research_node_id",
                        column: x => x.research_node_id,
                        principalTable: "research_nodes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "application",
                columns: table => new
                {
                    application_id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    app_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    additional_info = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_application", x => x.application_id);
                    table.ForeignKey(
                        name: "FK_application_research_research_id",
                        column: x => x.research_id,
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device",
                columns: table => new
                {
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    manufacturer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    additional_info = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device", x => x.device_id);
                    table.ForeignKey(
                        name: "FK_device_research_research_id",
                        column: x => x.research_id,
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "research_researcher",
                columns: table => new
                {
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    researcher_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_principal = table.Column<bool>(type: "boolean", nullable: false),
                    assigned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    removed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_researcher", x => new { x.research_id, x.researcher_id });
                    table.ForeignKey(
                        name: "FK_research_researcher_research_research_id",
                        column: x => x.research_id,
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_research_researcher_researcher_researcher_id",
                        column: x => x.researcher_id,
                        principalTable: "researcher",
                        principalColumn: "researcher_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "snomed_body_structure",
                columns: table => new
                {
                    snomed_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    body_region_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    structure_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_structure_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_snomed_body_structure", x => x.snomed_code);
                    table.ForeignKey(
                        name: "FK_snomed_body_structure_snomed_body_region_body_region_code",
                        column: x => x.body_region_code,
                        principalTable: "snomed_body_region",
                        principalColumn: "snomed_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_snomed_body_structure_snomed_body_structure_parent_structur~",
                        column: x => x.parent_structure_code,
                        principalTable: "snomed_body_structure",
                        principalColumn: "snomed_code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "record_session",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    volunteer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    clinical_context = table.Column<string>(type: "text", nullable: false),
                    start_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record_session", x => x.id);
                    table.ForeignKey(
                        name: "FK_record_session_research_research_id",
                        column: x => x.research_id,
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_record_session_volunteer_volunteer_id",
                        column: x => x.volunteer_id,
                        principalTable: "volunteer",
                        principalColumn: "volunteer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "research_volunteer",
                columns: table => new
                {
                    research_id = table.Column<Guid>(type: "uuid", nullable: false),
                    volunteer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    enrollment_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    consent_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    consent_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    exclusion_reason = table.Column<string>(type: "text", nullable: true),
                    enrolled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    withdrawn_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_research_volunteer", x => new { x.research_id, x.volunteer_id });
                    table.ForeignKey(
                        name: "FK_research_volunteer_research_research_id",
                        column: x => x.research_id,
                        principalTable: "research",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_research_volunteer_volunteer_volunteer_id",
                        column: x => x.volunteer_id,
                        principalTable: "volunteer",
                        principalColumn: "volunteer_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sensor",
                columns: table => new
                {
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    device_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    max_sampling_rate = table.Column<float>(type: "real", nullable: false),
                    unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    min_range = table.Column<float>(type: "real", nullable: false),
                    max_range = table.Column<float>(type: "real", nullable: false),
                    accuracy = table.Column<float>(type: "real", nullable: false),
                    additional_info = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sensor", x => x.sensor_id);
                    table.ForeignKey(
                        name: "FK_sensor_device_device_id",
                        column: x => x.device_id,
                        principalTable: "device",
                        principalColumn: "device_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "record",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    session_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    record_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record", x => x.id);
                    table.ForeignKey(
                        name: "FK_record_record_session_record_session_id",
                        column: x => x.record_session_id,
                        principalTable: "record_session",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "record_channel",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sensor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    signal_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_url = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    sampling_rate = table.Column<float>(type: "real", nullable: false),
                    samples_count = table.Column<int>(type: "integer", nullable: false),
                    start_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    annotations = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_record_channel", x => x.id);
                    table.ForeignKey(
                        name: "FK_record_channel_record_record_id",
                        column: x => x.record_id,
                        principalTable: "record",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_record_channel_sensor_sensor_id",
                        column: x => x.sensor_id,
                        principalTable: "sensor",
                        principalColumn: "sensor_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "target_area",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    record_channel_id = table.Column<Guid>(type: "uuid", nullable: false),
                    body_structure_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    laterality_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    topographical_modifier_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "text", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_target_area", x => x.id);
                    table.ForeignKey(
                        name: "FK_target_area_record_channel_record_channel_id",
                        column: x => x.record_channel_id,
                        principalTable: "record_channel",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_target_area_snomed_body_structure_body_structure_code",
                        column: x => x.body_structure_code,
                        principalTable: "snomed_body_structure",
                        principalColumn: "snomed_code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_target_area_snomed_laterality_laterality_code",
                        column: x => x.laterality_code,
                        principalTable: "snomed_laterality",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_target_area_snomed_topographical_modifier_topographical_mod~",
                        column: x => x.topographical_modifier_code,
                        principalTable: "snomed_topographical_modifier",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_application_research_id",
                table: "application",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_device_research_id",
                table: "device",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_collection_date",
                table: "record",
                column: "collection_date");

            migrationBuilder.CreateIndex(
                name: "ix_record_record_session_id",
                table: "record",
                column: "record_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_record_type",
                table: "record",
                column: "record_type");

            migrationBuilder.CreateIndex(
                name: "ix_record_channel_record_id",
                table: "record_channel",
                column: "record_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_channel_sensor_id",
                table: "record_channel",
                column: "sensor_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_channel_signal_type",
                table: "record_channel",
                column: "signal_type");

            migrationBuilder.CreateIndex(
                name: "ix_record_session_research_id",
                table: "record_session",
                column: "research_id");

            migrationBuilder.CreateIndex(
                name: "ix_record_session_start_at",
                table: "record_session",
                column: "start_at");

            migrationBuilder.CreateIndex(
                name: "ix_record_session_volunteer_id",
                table: "record_session",
                column: "volunteer_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_node_id",
                table: "research",
                column: "research_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_start_date",
                table: "research",
                column: "start_date");

            migrationBuilder.CreateIndex(
                name: "ix_research_status",
                table: "research",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_research_researcher_assigned_at",
                table: "research_researcher",
                column: "assigned_at");

            migrationBuilder.CreateIndex(
                name: "ix_research_researcher_is_principal",
                table: "research_researcher",
                column: "is_principal");

            migrationBuilder.CreateIndex(
                name: "IX_research_researcher_researcher_id",
                table: "research_researcher",
                column: "researcher_id");

            migrationBuilder.CreateIndex(
                name: "ix_research_volunteer_enrolled_at",
                table: "research_volunteer",
                column: "enrolled_at");

            migrationBuilder.CreateIndex(
                name: "ix_research_volunteer_enrollment_status",
                table: "research_volunteer",
                column: "enrollment_status");

            migrationBuilder.CreateIndex(
                name: "IX_research_volunteer_volunteer_id",
                table: "research_volunteer",
                column: "volunteer_id");

            migrationBuilder.CreateIndex(
                name: "ix_researcher_email",
                table: "researcher",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_researcher_research_node_id",
                table: "researcher",
                column: "research_node_id");

            migrationBuilder.CreateIndex(
                name: "ix_sensor_device_id",
                table: "sensor",
                column: "device_id");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_body_region_is_active",
                table: "snomed_body_region",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_body_region_parent_region_code",
                table: "snomed_body_region",
                column: "parent_region_code");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_body_structure_body_region_code",
                table: "snomed_body_structure",
                column: "body_region_code");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_body_structure_is_active",
                table: "snomed_body_structure",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_body_structure_parent_structure_code",
                table: "snomed_body_structure",
                column: "parent_structure_code");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_body_structure_structure_type",
                table: "snomed_body_structure",
                column: "structure_type");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_laterality_is_active",
                table: "snomed_laterality",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_topographical_modifier_category",
                table: "snomed_topographical_modifier",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_snomed_topographical_modifier_is_active",
                table: "snomed_topographical_modifier",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_target_area_body_structure_code",
                table: "target_area",
                column: "body_structure_code");

            migrationBuilder.CreateIndex(
                name: "IX_target_area_laterality_code",
                table: "target_area",
                column: "laterality_code");

            migrationBuilder.CreateIndex(
                name: "ix_target_area_record_channel_id",
                table: "target_area",
                column: "record_channel_id");

            migrationBuilder.CreateIndex(
                name: "IX_target_area_topographical_modifier_code",
                table: "target_area",
                column: "topographical_modifier_code");

            migrationBuilder.CreateIndex(
                name: "ix_volunteer_code",
                table: "volunteer",
                column: "volunteer_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_volunteer_consent_status",
                table: "volunteer",
                column: "consent_status");

            migrationBuilder.CreateIndex(
                name: "ix_volunteer_research_node_id",
                table: "volunteer",
                column: "research_node_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "application");

            migrationBuilder.DropTable(
                name: "research_researcher");

            migrationBuilder.DropTable(
                name: "research_volunteer");

            migrationBuilder.DropTable(
                name: "target_area");

            migrationBuilder.DropTable(
                name: "researcher");

            migrationBuilder.DropTable(
                name: "record_channel");

            migrationBuilder.DropTable(
                name: "snomed_body_structure");

            migrationBuilder.DropTable(
                name: "snomed_laterality");

            migrationBuilder.DropTable(
                name: "snomed_topographical_modifier");

            migrationBuilder.DropTable(
                name: "record");

            migrationBuilder.DropTable(
                name: "sensor");

            migrationBuilder.DropTable(
                name: "snomed_body_region");

            migrationBuilder.DropTable(
                name: "record_session");

            migrationBuilder.DropTable(
                name: "device");

            migrationBuilder.DropTable(
                name: "volunteer");

            migrationBuilder.DropTable(
                name: "research");
        }
    }
}
