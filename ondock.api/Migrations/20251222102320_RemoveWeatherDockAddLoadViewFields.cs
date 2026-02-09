using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWeatherDockAddLoadViewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DockSubscriptions");

            migrationBuilder.DropTable(
                name: "WeatherCaches");

            migrationBuilder.DropTable(
                name: "Docks");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "RoadRequests",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "WasHelpful",
                table: "RoadRequestResponses",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AddColumn<bool>(
                name: "IsLiveStream",
                table: "RoadRequestResponses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "LiveKitRoomName",
                table: "RoadRequestResponses",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RewardPoints",
                table: "RoadRequestResponses",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ThumbnailUrl",
                table: "RoadRequestResponses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RoadRequests_ExpiresAt",
                table: "RoadRequests",
                column: "ExpiresAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RoadRequests_ExpiresAt",
                table: "RoadRequests");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "RoadRequests");

            migrationBuilder.DropColumn(
                name: "IsLiveStream",
                table: "RoadRequestResponses");

            migrationBuilder.DropColumn(
                name: "LiveKitRoomName",
                table: "RoadRequestResponses");

            migrationBuilder.DropColumn(
                name: "RewardPoints",
                table: "RoadRequestResponses");

            migrationBuilder.DropColumn(
                name: "ThumbnailUrl",
                table: "RoadRequestResponses");

            migrationBuilder.AlterColumn<bool>(
                name: "WasHelpful",
                table: "RoadRequestResponses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "Docks",
                columns: table => new
                {
                    DockId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    LastStatusUpdate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    Location = table.Column<Point>(type: "geography(Point)", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docks", x => x.DockId);
                    table.ForeignKey(
                        name: "FK_Docks_AspNetUsers_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WeatherCaches",
                columns: table => new
                {
                    LocationHash = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Conditions = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Humidity = table.Column<float>(type: "real", nullable: false),
                    Pressure = table.Column<float>(type: "real", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeatherCaches", x => x.LocationHash);
                });

            migrationBuilder.CreateTable(
                name: "DockSubscriptions",
                columns: table => new
                {
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DockId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    NotificationEnabled = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DockSubscriptions", x => x.SubscriptionId);
                    table.ForeignKey(
                        name: "FK_DockSubscriptions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DockSubscriptions_Docks_DockId",
                        column: x => x.DockId,
                        principalTable: "Docks",
                        principalColumn: "DockId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DockSubscriptions_DockId",
                table: "DockSubscriptions",
                column: "DockId");

            migrationBuilder.CreateIndex(
                name: "IX_DockSubscriptions_UserId_DockId",
                table: "DockSubscriptions",
                columns: new[] { "UserId", "DockId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Docks_CreatedBy",
                table: "Docks",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Docks_Location",
                table: "Docks",
                column: "Location")
                .Annotation("Npgsql:IndexMethod", "gist");
        }
    }
}
