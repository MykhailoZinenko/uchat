using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uchat_server.Migrations
{
    /// <inheritdoc />
    public partial class RenameSessionTokenToRefreshToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_Token",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "Token",
                table: "Sessions");

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Sessions",
                type: "TEXT",
                maxLength: 45,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RefreshToken",
                table: "Sessions",
                type: "TEXT",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_RefreshToken",
                table: "Sessions",
                column: "RefreshToken",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_RefreshToken",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Sessions");

            migrationBuilder.AddColumn<string>(
                name: "Token",
                table: "Sessions",
                type: "TEXT",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_Token",
                table: "Sessions",
                column: "Token",
                unique: true);
        }
    }
}
