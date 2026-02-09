using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPostsAndViews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserPosts",
                columns: table => new
                {
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostType = table.Column<int>(type: "integer", nullable: false),
                    TextContent = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    BackgroundColor = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BackgroundImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TextPositionX = table.Column<float>(type: "real", nullable: true),
                    TextPositionY = table.Column<float>(type: "real", nullable: true),
                    MediaUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ThumbnailUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPosts", x => x.PostId);
                    table.ForeignKey(
                        name: "FK_UserPosts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PostViews",
                columns: table => new
                {
                    ViewId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ViewedAt = table.Column<DateTime>(type: "timestamp without time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostViews", x => x.ViewId);
                    table.ForeignKey(
                        name: "FK_PostViews_AspNetUsers_ViewerId",
                        column: x => x.ViewerId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostViews_UserPosts_PostId",
                        column: x => x.PostId,
                        principalTable: "UserPosts",
                        principalColumn: "PostId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostViews_PostId_ViewerId",
                table: "PostViews",
                columns: new[] { "PostId", "ViewerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostViews_ViewerId",
                table: "PostViews",
                column: "ViewerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPosts_ExpiresAt",
                table: "UserPosts",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserPosts_UserId_IsActive_ExpiresAt",
                table: "UserPosts",
                columns: new[] { "UserId", "IsActive", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostViews");

            migrationBuilder.DropTable(
                name: "UserPosts");
        }
    }
}
