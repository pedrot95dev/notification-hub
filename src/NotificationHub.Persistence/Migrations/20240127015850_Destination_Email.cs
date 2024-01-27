using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Destination_Email : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EmailDestination",
                table: "Applications",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDestination",
                table: "Applications");
        }
    }
}
