using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "InactiveAtUtc",
                table: "EmployeeAccounts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE [EmployeeAccounts] SET [InactiveAtUtc] = SYSUTCDATETIME() WHERE [IsActive] = 0");

            migrationBuilder.CreateTable(
                name: "CandidateProfiles",
                columns: table => new
                {
                    EmployeeAccountId = table.Column<int>(type: "int", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Experience = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    PerformanceScore = table.Column<int>(type: "int", nullable: false),
                    ResumeContent = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    ResumeFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ResumeContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CandidateProfiles", x => x.EmployeeAccountId);
                    table.ForeignKey(
                        name: "FK_CandidateProfiles_EmployeeAccounts_EmployeeAccountId",
                        column: x => x.EmployeeAccountId,
                        principalTable: "EmployeeAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.Sql(
                "INSERT INTO [CandidateProfiles] ([EmployeeAccountId], [FirstName], [LastName], [PhoneNumber], [Experience], [PerformanceScore]) " +
                "SELECT [Id], LEFT([DisplayName], CHARINDEX(N' ', [DisplayName] + N' ') - 1), " +
                "LTRIM(SUBSTRING([DisplayName], CHARINDEX(N' ', [DisplayName] + N' ') + 1, 100)), N'', N'', 0 " +
                "FROM [EmployeeAccounts] WHERE [Role] = N'Candidate'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CandidateProfiles");

            migrationBuilder.DropColumn(
                name: "InactiveAtUtc",
                table: "EmployeeAccounts");
        }
    }
}
