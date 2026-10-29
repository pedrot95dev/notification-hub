using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NotificationHub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EmailSentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "EmailSents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SentAt",
                table: "EmailSents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "EmailSents",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Error",
                table: "EmailSents");

            migrationBuilder.DropColumn(
                name: "SentAt",
                table: "EmailSents");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "EmailSents");
        }
    }
}
