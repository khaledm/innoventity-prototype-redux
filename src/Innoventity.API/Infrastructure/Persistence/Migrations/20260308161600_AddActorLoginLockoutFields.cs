using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innoventity.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddActorLoginLockoutFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Actors') AND name = 'FailedLoginAttempts')
                BEGIN
                    ALTER TABLE [Actors] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;
                END");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'Actors') AND name = 'LockoutUntil')
                BEGIN
                    ALTER TABLE [Actors] ADD [LockoutUntil] datetimeoffset NULL;
                END");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "Actors");

            migrationBuilder.DropColumn(
                name: "LockoutUntil",
                table: "Actors");
        }
    }
}
