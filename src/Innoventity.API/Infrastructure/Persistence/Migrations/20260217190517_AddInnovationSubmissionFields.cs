using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Innoventity.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInnovationSubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PartnersNeeded",
                table: "Innovations",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PotentialMarketSize",
                table: "Innovations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RelevantMarketSize",
                table: "Innovations",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetBeneficiaries",
                table: "Innovations",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TechnologyDescription",
                table: "Innovations",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            // Note: Industry seed data already exists from initial migration
            // migrationBuilder.InsertData(...) removed to prevent duplicate key errors
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: Industry seed data deletions removed since inserts were removed from Up()

            migrationBuilder.DropColumn(
                name: "PartnersNeeded",
                table: "Innovations");

            migrationBuilder.DropColumn(
                name: "PotentialMarketSize",
                table: "Innovations");

            migrationBuilder.DropColumn(
                name: "RelevantMarketSize",
                table: "Innovations");

            migrationBuilder.DropColumn(
                name: "TargetBeneficiaries",
                table: "Innovations");

            migrationBuilder.DropColumn(
                name: "TechnologyDescription",
                table: "Innovations");
        }
    }
}
