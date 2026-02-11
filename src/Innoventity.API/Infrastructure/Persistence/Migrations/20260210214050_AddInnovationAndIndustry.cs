using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innoventity.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInnovationAndIndustry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Industries",
                columns: table => new
                {
                    IndustryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Industries", x => x.IndustryId);
                });

            migrationBuilder.CreateTable(
                name: "Innovations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdeaToken = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ResearchBackground = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResearchCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IprStatus = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProductDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ProductAdvantages = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    DevelopmentPhase = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DevelopmentProcess = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetMarket = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetCustomerBase = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    TargetCustomerType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProductKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    AdvantageKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Innovations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Innovations_Actors_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InnovationIndustry",
                columns: table => new
                {
                    IndustryId = table.Column<string>(type: "nvarchar(50)", nullable: false),
                    InnovationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InnovationIndustry", x => new { x.IndustryId, x.InnovationId });
                    table.ForeignKey(
                        name: "FK_InnovationIndustry_Industries_IndustryId",
                        column: x => x.IndustryId,
                        principalTable: "Industries",
                        principalColumn: "IndustryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InnovationIndustry_Innovations_InnovationId",
                        column: x => x.InnovationId,
                        principalTable: "Innovations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InnovationIndustry_InnovationId",
                table: "InnovationIndustry",
                column: "InnovationId");

            migrationBuilder.CreateIndex(
                name: "IX_Innovations_OwnerId",
                table: "Innovations",
                column: "OwnerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InnovationIndustry");

            migrationBuilder.DropTable(
                name: "Industries");

            migrationBuilder.DropTable(
                name: "Innovations");
        }
    }
}
