using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace fullstackapplication.Server.Migrations
{
    /// <inheritdoc />
    public partial class AddCandidateYearsOfExperience : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Experience",
                table: "CandidateProfiles",
                newName: "LegacyExperienceDetails");

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "CandidateProfiles",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "CandidateProfiles");

            migrationBuilder.RenameColumn(
                name: "LegacyExperienceDetails",
                table: "CandidateProfiles",
                newName: "Experience");
        }
    }
}
