using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeAccessRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByEmployeeAccountId",
                table: "EmployeeAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RoleDefinitionId",
                table: "EmployeeAccounts",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmployeeRoleDefinitions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CreatedByEmployeeAccountId = table.Column<int>(type: "int", nullable: true),
                    IsBuiltIn = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRoleDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeRoleDefinitions_EmployeeAccounts_CreatedByEmployeeAccountId",
                        column: x => x.CreatedByEmployeeAccountId,
                        principalTable: "EmployeeAccounts",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "EmployeeRolePermissions",
                columns: table => new
                {
                    EmployeeRoleDefinitionId = table.Column<int>(type: "int", nullable: false),
                    PermissionCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeRolePermissions", x => new { x.EmployeeRoleDefinitionId, x.PermissionCode });
                    table.ForeignKey(
                        name: "FK_EmployeeRolePermissions_EmployeeRoleDefinitions_EmployeeRoleDefinitionId",
                        column: x => x.EmployeeRoleDefinitionId,
                        principalTable: "EmployeeRoleDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccounts_CreatedByEmployeeAccountId",
                table: "EmployeeAccounts",
                column: "CreatedByEmployeeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAccounts_RoleDefinitionId",
                table: "EmployeeAccounts",
                column: "RoleDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleDefinitions_CreatedByEmployeeAccountId",
                table: "EmployeeRoleDefinitions",
                column: "CreatedByEmployeeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeRoleDefinitions_Name",
                table: "EmployeeRoleDefinitions",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAccounts_EmployeeAccounts_CreatedByEmployeeAccountId",
                table: "EmployeeAccounts",
                column: "CreatedByEmployeeAccountId",
                principalTable: "EmployeeAccounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_EmployeeAccounts_EmployeeRoleDefinitions_RoleDefinitionId",
                table: "EmployeeAccounts",
                column: "RoleDefinitionId",
                principalTable: "EmployeeRoleDefinitions",
                principalColumn: "Id");

            migrationBuilder.Sql("""
                INSERT INTO EmployeeRoleDefinitions (Name, CreatedByEmployeeAccountId, IsBuiltIn)
                VALUES (N'Mentor', NULL, 1);
                DECLARE @MentorRoleId int = SCOPE_IDENTITY();
                INSERT INTO EmployeeRolePermissions (EmployeeRoleDefinitionId, PermissionCode)
                VALUES
                    (@MentorRoleId, N'Candidates.View'),
                    (@MentorRoleId, N'Candidates.Create'),
                    (@MentorRoleId, N'Candidates.Update'),
                    (@MentorRoleId, N'Candidates.Deactivate'),
                    (@MentorRoleId, N'Reviews.Work'),
                    (@MentorRoleId, N'Reviews.InterviewDecision');
                UPDATE EmployeeAccounts SET RoleDefinitionId = @MentorRoleId WHERE Role = N'Employee';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAccounts_EmployeeAccounts_CreatedByEmployeeAccountId",
                table: "EmployeeAccounts");

            migrationBuilder.DropForeignKey(
                name: "FK_EmployeeAccounts_EmployeeRoleDefinitions_RoleDefinitionId",
                table: "EmployeeAccounts");

            migrationBuilder.DropTable(
                name: "EmployeeRolePermissions");

            migrationBuilder.DropTable(
                name: "EmployeeRoleDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAccounts_CreatedByEmployeeAccountId",
                table: "EmployeeAccounts");

            migrationBuilder.DropIndex(
                name: "IX_EmployeeAccounts_RoleDefinitionId",
                table: "EmployeeAccounts");

            migrationBuilder.DropColumn(
                name: "CreatedByEmployeeAccountId",
                table: "EmployeeAccounts");

            migrationBuilder.DropColumn(
                name: "RoleDefinitionId",
                table: "EmployeeAccounts");
        }
    }
}
