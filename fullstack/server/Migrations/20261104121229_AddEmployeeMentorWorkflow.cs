using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeMentorWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "RelatedWorkEntryId",
                table: "UserNotifications",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "EmployeeAccounts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Stack",
                table: "EmployeeAccounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReviewedByEmployeeAccountId",
                table: "CandidateWorkEntries",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatorEmployeeAccountId",
                table: "CandidateProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CandidateWorkEntries_ReviewedByEmployeeAccountId",
                table: "CandidateWorkEntries",
                column: "ReviewedByEmployeeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_CandidateProfiles_CreatorEmployeeAccountId",
                table: "CandidateProfiles",
                column: "CreatorEmployeeAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateProfiles_EmployeeAccounts_CreatorEmployeeAccountId",
                table: "CandidateProfiles",
                column: "CreatorEmployeeAccountId",
                principalTable: "EmployeeAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_CandidateWorkEntries_EmployeeAccounts_ReviewedByEmployeeAccountId",
                table: "CandidateWorkEntries",
                column: "ReviewedByEmployeeAccountId",
                principalTable: "EmployeeAccounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CandidateProfiles_EmployeeAccounts_CreatorEmployeeAccountId",
                table: "CandidateProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_CandidateWorkEntries_EmployeeAccounts_ReviewedByEmployeeAccountId",
                table: "CandidateWorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_CandidateWorkEntries_ReviewedByEmployeeAccountId",
                table: "CandidateWorkEntries");

            migrationBuilder.DropIndex(
                name: "IX_CandidateProfiles_CreatorEmployeeAccountId",
                table: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "EmployeeAccounts");

            migrationBuilder.DropColumn(
                name: "Stack",
                table: "EmployeeAccounts");

            migrationBuilder.DropColumn(
                name: "ReviewedByEmployeeAccountId",
                table: "CandidateWorkEntries");

            migrationBuilder.DropColumn(
                name: "CreatorEmployeeAccountId",
                table: "CandidateProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "RelatedWorkEntryId",
                table: "UserNotifications",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
