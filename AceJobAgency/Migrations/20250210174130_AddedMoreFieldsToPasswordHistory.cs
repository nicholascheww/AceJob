using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AceJobAgency.Migrations
{
    /// <inheritdoc />
    public partial class AddedMoreFieldsToPasswordHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "HashedPassword",
                table: "PasswordHistory",
                newName: "PasswordHash");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "PasswordHistory",
                newName: "DateChanged");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "PasswordHash",
                table: "PasswordHistory",
                newName: "HashedPassword");

            migrationBuilder.RenameColumn(
                name: "DateChanged",
                table: "PasswordHistory",
                newName: "CreatedAt");
        }
    }
}
