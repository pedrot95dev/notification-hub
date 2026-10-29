using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueApplicationExternalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Applications_ExternalId",
                table: "Applications",
                column: "ExternalId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_ExternalId",
                table: "Applications");
        }
    }
}
