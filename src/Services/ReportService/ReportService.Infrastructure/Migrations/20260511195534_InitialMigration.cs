using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "report");

            migrationBuilder.CreateTable(
                name: "analysis_reports",
                schema: "report",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisRequestId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    analysis_data = table.Column<string>(type: "jsonb", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_reports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "analysis_report_files",
                schema: "report",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AnalysisReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    Format = table.Column<int>(type: "integer", nullable: false),
                    bucket_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_report_files", x => x.Id);
                    table.ForeignKey(
                        name: "FK_analysis_report_files_analysis_reports_AnalysisReportId",
                        column: x => x.AnalysisReportId,
                        principalSchema: "report",
                        principalTable: "analysis_reports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_report_files_AnalysisReportId_Format",
                schema: "report",
                table: "analysis_report_files",
                columns: new[] { "AnalysisReportId", "Format" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_analysis_report_files_Format",
                schema: "report",
                table: "analysis_report_files",
                column: "Format");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_reports_AnalysisRequestId",
                schema: "report",
                table: "analysis_reports",
                column: "AnalysisRequestId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_analysis_reports_CreatedAtUtc",
                schema: "report",
                table: "analysis_reports",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_reports_RequestedByUserId",
                schema: "report",
                table: "analysis_reports",
                column: "RequestedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_report_files",
                schema: "report");

            migrationBuilder.DropTable(
                name: "analysis_reports",
                schema: "report");
        }
    }
}
