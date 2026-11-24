using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class RemoveLibrarySettingDeletedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // LibrarySettings is a singleton editable config row. It is never
            // deleted, so the soft-delete column added in SoftDeleteAll is
            // unnecessary. DateTime values are stored as Unix seconds (bigint).
            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LibrarySettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "LibrarySettings",
                type: "bigint",
                nullable: true);
        }
    }
}
