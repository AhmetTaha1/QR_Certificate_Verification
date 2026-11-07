using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRCertificationSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSuperAdminPassword : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj5QJsWQQqOK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1,
                column: "PasswordHash",
                value: "$2a$11$rXQJTkqVr7M7G5z1W4K/t.LGXOy4NW3YnM8fhJw5Nf4r8g3z9x2y4");
        }
    }
}
