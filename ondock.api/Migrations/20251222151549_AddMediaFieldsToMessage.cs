using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFieldsToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MediaDurationSeconds",
                table: "Messages",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaFileName",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "MediaFileSize",
                table: "Messages",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaDurationSeconds",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaFileName",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaFileSize",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Messages");
        }
    }
}
