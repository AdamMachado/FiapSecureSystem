using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProcessingService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialProcessingSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "processing");

            migrationBuilder.CreateTable(
                name: "analysis_processes",
                schema: "processing",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_request_id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    source_bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    source_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    DiagramType = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    extracted_text = table.Column<string>(type: "text", nullable: true),
                    summary_overview = table.Column<string>(type: "text", nullable: true),
                    summary_total_components = table.Column<int>(type: "integer", nullable: true),
                    summary_total_risks = table.Column<int>(type: "integer", nullable: true),
                    summary_total_recommendations = table.Column<int>(type: "integer", nullable: true),
                    summary_requires_manual_review = table.Column<bool>(type: "boolean", nullable: true),
                    summary_warnings = table.Column<string>(type: "jsonb", nullable: true),
                    FailureReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FailureDetails = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    components = table.Column<string>(type: "jsonb", nullable: false),
                    recommendations = table.Column<string>(type: "jsonb", nullable: false),
                    risks = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_processes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_analysis_processes_analysis_request_id",
                schema: "processing",
                table: "analysis_processes",
                column: "analysis_request_id");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_processes_CreatedAtUtc",
                schema: "processing",
                table: "analysis_processes",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_processes_RequestedByUserId",
                schema: "processing",
                table: "analysis_processes",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_processes_Status",
                schema: "processing",
                table: "analysis_processes",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_processes",
                schema: "processing");
        }
    }
}
