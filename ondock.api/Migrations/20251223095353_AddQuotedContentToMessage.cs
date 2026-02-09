using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ondock.api.Migrations
{
    /// <inheritdoc />
    public partial class AddQuotedContentToMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "QuotedContent",
                table: "Messages",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuotedTitle",
                table: "Messages",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "QuotedContent",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "QuotedTitle",
                table: "Messages");
        }
    }
}
