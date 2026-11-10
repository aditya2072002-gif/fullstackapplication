using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateReviewStages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "EmployeeAccounts",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(day, -31, SYSUTCDATETIME())");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "CandidateWorkEntries",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<string>(
                name: "InterviewDecision",
                table: "CandidateProfiles",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PendingReview");

            migrationBuilder.AddColumn<DateTime>(
                name: "InterviewDecisionAtUtc",
                table: "CandidateProfiles",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InterviewDecisionByEmployeeAccountId",
                table: "CandidateProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InterviewDecisionComment",
                table: "CandidateProfiles",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.Sql("UPDATE CandidateWorkEntries SET Status = CASE WHEN Grade IS NOT NULL THEN N'Approved' ELSE N'Pending' END;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "EmployeeAccounts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "CandidateWorkEntries");

            migrationBuilder.DropColumn(
                name: "InterviewDecision",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "InterviewDecisionAtUtc",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "InterviewDecisionByEmployeeAccountId",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "InterviewDecisionComment",
                table: "CandidateProfiles");
        }
    }
}
