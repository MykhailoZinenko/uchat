using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace uchat_server.Migrations
{
    /// <inheritdoc />
    public partial class RenameRefreshTokenToSessionToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "RefreshToken",
                table: "Sessions",
                newName: "SessionToken");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_RefreshToken",
                table: "Sessions",
                newName: "IX_Sessions_SessionToken");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SessionToken",
                table: "Sessions",
                newName: "RefreshToken");

            migrationBuilder.RenameIndex(
                name: "IX_Sessions_SessionToken",
                table: "Sessions",
                newName: "IX_Sessions_RefreshToken");
        }
    }
}
