using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateApplicationSmtpConfigurationRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_SmtpConfigurationId",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_SmtpConfigurationId",
                table: "Applications",
                column: "SmtpConfigurationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Applications_SmtpConfigurationId",
                table: "Applications");

            migrationBuilder.CreateIndex(
                name: "IX_Applications_SmtpConfigurationId",
                table: "Applications",
                column: "SmtpConfigurationId",
                unique: true);
        }
    }
}
