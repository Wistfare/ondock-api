using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DNDMode",
                table: "ActiveTrucks");

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    SettingsId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DndEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    DndScheduleStart = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    DndScheduleEnd = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true),
                    DndScheduleEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    NotifyChat = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyAnnouncements = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyNearbyUsers = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyDockStatus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotifyRoadAlerts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotificationSound = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    NotificationVibration = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    VisibleToNearby = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShareLocationInChat = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowOnlineStatus = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ShowReadReceipts = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    DefaultChatRadiusMiles = table.Column<double>(type: "double precision", nullable: false, defaultValue: 0.5),
                    AutoJoinNearbyChats = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.SettingsId);
                    table.ForeignKey(
                        name: "FK_UserSettings_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserSettings_UserId",
                table: "UserSettings",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.AddColumn<bool>(
                name: "DNDMode",
                table: "ActiveTrucks",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
