using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReportService.Infrastructure.Migrations
{
    public partial class RefactorAnalysisReportStorage : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_analysis_reports_Status",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.AddColumn<string>(
                name: "analysis_data",
                schema: "report",
                table: "analysis_reports",
                type: "jsonb",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE report.analysis_reports
                SET analysis_data = jsonb_build_object('legacyMarkdown', content);
                """);

            migrationBuilder.AlterColumn<string>(
                name: "analysis_data",
                schema: "report",
                table: "analysis_reports",
                type: "jsonb",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldNullable: true);

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

            migrationBuilder.Sql(
                """
                INSERT INTO report.analysis_report_files
                    (Id, AnalysisReportId, Format, bucket_name, object_key, content_type, file_name, created_at_utc)
                SELECT
                    "Id",
                    "Id",
                    "Format",
                    storage_bucket,
                    storage_object_key,
                    content_type,
                    file_name,
                    COALESCE("GeneratedAtUtc", "CreatedAtUtc")
                FROM report.analysis_reports
                WHERE storage_bucket IS NOT NULL
                  AND storage_object_key IS NOT NULL
                  AND file_name IS NOT NULL
                  AND content_type IS NOT NULL;
                """);

            migrationBuilder.DropColumn(
                name: "content",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "content_type",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "failure_reason",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "file_name",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "Format",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "GeneratedAtUtc",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "storage_bucket",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.DropColumn(
                name: "storage_object_key",
                schema: "report",
                table: "analysis_reports");

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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_report_files",
                schema: "report");

            migrationBuilder.DropColumn(
                name: "analysis_data",
                schema: "report",
                table: "analysis_reports");

            migrationBuilder.AddColumn<string>(
                name: "content",
                schema: "report",
                table: "analysis_reports",
                type: "text",
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "content_type",
                schema: "report",
                table: "analysis_reports",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "failure_reason",
                schema: "report",
                table: "analysis_reports",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "file_name",
                schema: "report",
                table: "analysis_reports",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<int>(
                name: "Format",
                schema: "report",
                table: "analysis_reports",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<DateTime>(
                name: "GeneratedAtUtc",
                schema: "report",
                table: "analysis_reports",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                schema: "report",
                table: "analysis_reports",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "storage_bucket",
                schema: "report",
                table: "analysis_reports",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.AddColumn<string>(
                name: "storage_object_key",
                schema: "report",
                table: "analysis_reports",
                type: "character varying(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: string.Empty);

            migrationBuilder.CreateIndex(
                name: "IX_analysis_reports_Status",
                schema: "report",
                table: "analysis_reports",
                column: "Status");
        }
    }
}
