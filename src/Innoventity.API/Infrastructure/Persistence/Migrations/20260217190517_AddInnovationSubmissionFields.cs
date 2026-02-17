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

            migrationBuilder.InsertData(
                table: "Industries",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { "AUTO-001", "Consumer Goods" },
                    { "CSVC-001", "Consumer Services" },
                    { "ENRG-001", "Oil & Gas" },
                    { "FIN-001", "Financials" },
                    { "HLTH-001", "Health Care" },
                    { "INDU-001", "Industrials" },
                    { "MTRL-001", "Basic Materials" },
                    { "TCOM-001", "Telecommunications" },
                    { "TECH-001", "Technology" },
                    { "UTIL-001", "Utilities" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "AUTO-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "CSVC-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "ENRG-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "FIN-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "HLTH-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "INDU-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "MTRL-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "TCOM-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "TECH-001");

            migrationBuilder.DeleteData(
                table: "Industries",
                keyColumn: "Id",
                keyValue: "UTIL-001");

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
