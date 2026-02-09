using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class RenameRoadPostsToPost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename table from RoadPosts to Posts
            migrationBuilder.RenameTable(
                name: "RoadPosts",
                newName: "Posts");

            // Rename indexes
            migrationBuilder.RenameIndex(
                name: "IX_RoadPosts_UserId",
                table: "Posts",
                newName: "IX_Posts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_RoadPosts_RoadIdentification",
                table: "Posts",
                newName: "IX_Posts_RoadIdentification");

            migrationBuilder.RenameIndex(
                name: "IX_RoadPosts_Location",
                table: "Posts",
                newName: "IX_Posts_Location");

            migrationBuilder.RenameIndex(
                name: "IX_RoadPosts_CreatedAt_ExpiresAt",
                table: "Posts",
                newName: "IX_Posts_CreatedAt_ExpiresAt");

            // Rename primary key constraint
            migrationBuilder.Sql(@"ALTER TABLE ""Posts"" RENAME CONSTRAINT ""PK_RoadPosts"" TO ""PK_Posts"";");

            // Rename foreign key constraint
            migrationBuilder.Sql(@"ALTER TABLE ""Posts"" RENAME CONSTRAINT ""FK_RoadPosts_AspNetUsers_UserId"" TO ""FK_Posts_AspNetUsers_UserId"";");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rename constraints back
            migrationBuilder.Sql(@"ALTER TABLE ""Posts"" RENAME CONSTRAINT ""PK_Posts"" TO ""PK_RoadPosts"";");
            migrationBuilder.Sql(@"ALTER TABLE ""Posts"" RENAME CONSTRAINT ""FK_Posts_AspNetUsers_UserId"" TO ""FK_RoadPosts_AspNetUsers_UserId"";");

            // Rename indexes back
            migrationBuilder.RenameIndex(
                name: "IX_Posts_UserId",
                table: "Posts",
                newName: "IX_RoadPosts_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_RoadIdentification",
                table: "Posts",
                newName: "IX_RoadPosts_RoadIdentification");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_Location",
                table: "Posts",
                newName: "IX_RoadPosts_Location");

            migrationBuilder.RenameIndex(
                name: "IX_Posts_CreatedAt_ExpiresAt",
                table: "Posts",
                newName: "IX_RoadPosts_CreatedAt_ExpiresAt");

            // Rename table back
            migrationBuilder.RenameTable(
                name: "Posts",
                newName: "RoadPosts");
        }
    }
}
