using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateWorkTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Stack",
                table: "CandidateProfiles",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CandidateWorkEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateProfileId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Task = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HoursSpent = table.Column<decimal>(type: "decimal(4,1)", precision: 4, scale: 1, nullable: false),
                    Accomplishment = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Learnings = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    AiToolsUsed = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Grade = table.Column<int>(type: "int", nullable: true),
                    AdminFeedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateWorkEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CandidateWorkEntries_CandidateProfiles_CandidateProfileId",
                        column: x => x.CandidateProfileId,
                        principalTable: "CandidateProfiles",
                        principalColumn: "EmployeeAccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CandidateWorkEntries_CandidateProfileId_WorkDate",
                table: "CandidateWorkEntries",
                columns: new[] { "CandidateProfileId", "WorkDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateWorkEntries");

            migrationBuilder.DropColumn(
                name: "Stack",
                table: "CandidateProfiles");
        }
    }
}
