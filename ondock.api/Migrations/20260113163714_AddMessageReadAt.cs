using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageReadAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ReadAt",
                table: "Messages",
                type: "timestamp without time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadAt",
                table: "Messages");
        }
    }
}
