using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Innoventity.API.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveAccountStatusDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccountStatus",
                table: "Actors",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldDefaultValue: "PendingActivation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AccountStatus",
                table: "Actors",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "PendingActivation",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");
        }
    }
}
