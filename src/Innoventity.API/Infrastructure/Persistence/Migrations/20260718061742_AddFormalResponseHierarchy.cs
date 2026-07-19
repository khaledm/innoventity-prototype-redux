using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innoventity.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFormalResponseHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FormalResponses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ParticipationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ParticipationProposal = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ResponseType = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    Feedback = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ProductDevelopmentDuration = table.Column<int>(type: "int", nullable: true),
                    YearlyDevelopmentCosts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearlyManufacturingCosts = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearlySales = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormalResponses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FormalResponses_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FormalResponses_Innovations_InnovationId",
                        column: x => x.InnovationId,
                        principalTable: "Innovations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Data migration (Spec 005 research.md Decision 4 / data-model.md): copy existing Bid
            // rows into FormalResponses before the Bids table is dropped. ActorType (via Actors
            // join) maps to the ResponseType discriminator. Bid.Location was free text (e.g.
            // "Munich, Germany"); it is best-effort mapped to the closest GeographicRegion value
            // since exact geocoding is out of scope — acceptable per plan.md Constraint 2 (no
            // production data exists yet, seed/test data only). Migrated financial responses get
            // empty JSON projection arrays (Bid never captured structured projection data).
            migrationBuilder.Sql(@"
                INSERT INTO FormalResponses
                    (Id, InnovationId, ActorId, Location, ParticipationType, ParticipationProposal,
                     Status, SubmittedAt, UpdatedAt, AcceptedAt, ResponseType,
                     YearlyManufacturingCosts, YearlySales, YearlyDevelopmentCosts,
                     ProductDevelopmentDuration, Feedback)
                SELECT
                    b.Id, b.InnovationId, b.ActorId,
                    CASE
                        WHEN b.Location LIKE '%German%' OR b.Location LIKE '%UK%' OR b.Location LIKE '%Britain%' OR b.Location LIKE '%Europe%' THEN 'Europe'
                        WHEN b.Location LIKE '%USA%' OR b.Location LIKE '%US%' OR b.Location LIKE '%America%' OR b.Location LIKE '%Canada%' THEN 'Americas'
                        WHEN b.Location LIKE '%China%' OR b.Location LIKE '%Japan%' OR b.Location LIKE '%India%' OR b.Location LIKE '%Asia%' THEN 'Asia'
                        WHEN b.Location LIKE '%Africa%' THEN 'Africa'
                        WHEN b.Location LIKE '%Australia%' OR b.Location LIKE '%Oceania%' OR b.Location LIKE '%Zealand%' THEN 'Oceania'
                        ELSE 'Europe'
                    END,
                    b.ParticipationType, b.ParticipationProposal, b.Status, b.SubmittedAt, b.UpdatedAt, b.AcceptedAt,
                    CASE a.ActorType
                        WHEN 'Manufacturing' THEN 'ManufacturingResponse'
                        WHEN 'SalesMarketing' THEN 'SalesMarketingResponse'
                        WHEN 'RD' THEN 'ResearchDevelopmentResponse'
                        ELSE 'InvestorResponse'
                    END,
                    CASE WHEN a.ActorType = 'Manufacturing' THEN '[]' ELSE NULL END,
                    CASE WHEN a.ActorType = 'SalesMarketing' THEN '[]' ELSE NULL END,
                    CASE WHEN a.ActorType = 'RD' THEN '[]' ELSE NULL END,
                    CASE WHEN a.ActorType = 'RD' THEN 0 ELSE NULL END,
                    CASE WHEN a.ActorType NOT IN ('Manufacturing', 'SalesMarketing', 'RD') THEN 'Migrated from legacy Bid record during the FormalResponse hierarchy rollout; no structured feedback was captured.' ELSE NULL END
                FROM Bids b
                INNER JOIN Actors a ON a.Id = b.ActorId;
            ");

            migrationBuilder.DropTable(
                name: "Bids");

            migrationBuilder.CreateIndex(
                name: "IX_FormalResponse_ActorId_InnovationId",
                table: "FormalResponses",
                columns: new[] { "ActorId", "InnovationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormalResponses_InnovationId",
                table: "FormalResponses",
                column: "InnovationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Bids",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InnovationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParticipationProposal = table.Column<string>(type: "nvarchar(max)", maxLength: 5000, nullable: false),
                    ParticipationType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bids", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bids_Actors_ActorId",
                        column: x => x.ActorId,
                        principalTable: "Actors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Bids_Innovations_InnovationId",
                        column: x => x.InnovationId,
                        principalTable: "Innovations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Reverse data migration: move FormalResponse rows back into Bids before the table is
            // dropped. The Location free-text column is reconstructed from the GeographicRegion
            // enum name (lossy relative to the original free text, acceptable — see research.md
            // Decision 4/5, no production data exists yet).
            migrationBuilder.Sql(@"
                INSERT INTO Bids
                    (Id, ActorId, InnovationId, AcceptedAt, Location, ParticipationProposal,
                     ParticipationType, Status, SubmittedAt, UpdatedAt)
                SELECT
                    r.Id, r.ActorId, r.InnovationId, r.AcceptedAt, r.Location, r.ParticipationProposal,
                    r.ParticipationType, r.Status, r.SubmittedAt, r.UpdatedAt
                FROM FormalResponses r;
            ");

            migrationBuilder.DropTable(
                name: "FormalResponses");

            migrationBuilder.CreateIndex(
                name: "IX_Bid_ActorId_InnovationId",
                table: "Bids",
                columns: new[] { "ActorId", "InnovationId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bids_InnovationId",
                table: "Bids",
                column: "InnovationId");
        }
    }
}
