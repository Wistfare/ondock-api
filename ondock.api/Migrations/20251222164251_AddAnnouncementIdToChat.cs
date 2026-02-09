using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class AddAnnouncementIdToChat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AnnouncementId",
                table: "Chats",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Chats_AnnouncementId",
                table: "Chats",
                column: "AnnouncementId");

            migrationBuilder.AddForeignKey(
                name: "FK_Chats_Announcements_AnnouncementId",
                table: "Chats",
                column: "AnnouncementId",
                principalTable: "Announcements",
                principalColumn: "AnnouncementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Chats_Announcements_AnnouncementId",
                table: "Chats");

            migrationBuilder.DropIndex(
                name: "IX_Chats_AnnouncementId",
                table: "Chats");

            migrationBuilder.DropColumn(
                name: "AnnouncementId",
                table: "Chats");
        }
    }
}
