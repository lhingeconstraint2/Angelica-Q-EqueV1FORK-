using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DiscordEqueBot.Migrations
{
    /// <inheritdoc />
    public partial class ReplyTo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReplyTo",
                table: "Messages",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReplyTo",
                table: "Messages");
        }
    }
}
