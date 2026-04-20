using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UploadService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialUploadSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "upload");

            migrationBuilder.CreateTable(
                name: "analysis_requests",
                schema: "upload",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    size_in_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_type = table.Column<int>(type: "integer", nullable: false),
                    file_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    storage_bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_object_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FailedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_requests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_requests_CreatedAtUtc",
                schema: "upload",
                table: "analysis_requests",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_requests_RequestedByUserId",
                schema: "upload",
                table: "analysis_requests",
                column: "RequestedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_analysis_requests_Status",
                schema: "upload",
                table: "analysis_requests",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_requests",
                schema: "upload");
        }
    }
}
