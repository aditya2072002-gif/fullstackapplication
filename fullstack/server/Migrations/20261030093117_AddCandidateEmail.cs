using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "CandidateProfiles",
                type: "nvarchar(254)",
                maxLength: 254,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE profiles SET Email = LOWER(accounts.Username) + N'@development.invalid' " +
                "FROM CandidateProfiles AS profiles " +
                "INNER JOIN EmployeeAccounts AS accounts ON profiles.EmployeeAccountId = accounts.Id " +
                "WHERE profiles.Email = N''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Email",
                table: "CandidateProfiles");
        }
    }
}
