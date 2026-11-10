using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.EmployeeAccounts', N'ProfilePhotoContent') IS NULL " +
                "ALTER TABLE [dbo].[EmployeeAccounts] ADD [ProfilePhotoContent] varbinary(max) NULL;");

            migrationBuilder.Sql(
                "IF COL_LENGTH(N'dbo.EmployeeAccounts', N'ProfilePhotoContentType') IS NULL " +
                "ALTER TABLE [dbo].[EmployeeAccounts] ADD [ProfilePhotoContentType] nvarchar(100) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProfilePhotoContent",
                table: "EmployeeAccounts");

            migrationBuilder.DropColumn(
                name: "ProfilePhotoContentType",
                table: "EmployeeAccounts");
        }
    }
}
