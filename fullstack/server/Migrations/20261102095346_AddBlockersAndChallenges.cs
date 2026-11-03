using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockersAndChallenges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BlockersAndChallenges",
                table: "CandidateWorkEntries",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BlockersAndChallenges",
                table: "CandidateWorkEntries");
        }
    }
}
