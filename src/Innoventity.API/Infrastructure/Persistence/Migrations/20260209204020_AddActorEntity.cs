using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innoventity.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ContactAddress = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ActorType = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AccountStatus = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "PendingActivation"),
                    ActivationToken = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PasswordHash = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actor_Email_ActorType",
                table: "Actors",
                columns: new[] { "Email", "ActorType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actors");
        }
    }
}
