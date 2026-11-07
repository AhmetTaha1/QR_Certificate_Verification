using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QRCertificationSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededAdminUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "IsActive", "LastLoginAt", "LastName", "PasswordHash", "PhoneNumber", "ProfileImageUrl", "Role", "UniversityId", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "admin@system.com", true, "Super", true, null, "Admin", "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj5QJsWQQqOK", null, null, 0, null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }
    }
}
