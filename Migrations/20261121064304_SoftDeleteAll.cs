using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibraryManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class SoftDeleteAll : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_BookId_MemberId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_BookId_ReservedAt_ReservationId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_MembershipTypes_Name",
                table: "MembershipTypes");

            migrationBuilder.DropIndex(
                name: "IX_Loans_BookId_MemberId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Books_ISBN",
                table: "Books");

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Reservations",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "MembershipTypes",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Memberships",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Loans",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "LibrarySettings",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Categories",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "BookAuthors",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DeletedAt",
                table: "Authors",
                type: "bigint",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValue: 1,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValue: 2,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Authors",
                keyColumn: "AuthorId",
                keyValue: 3,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BookAuthors",
                keyColumns: new[] { "AuthorId", "BookId" },
                keyValues: new object[] { 1, 1 },
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BookAuthors",
                keyColumns: new[] { "AuthorId", "BookId" },
                keyValues: new object[] { 2, 2 },
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "BookAuthors",
                keyColumns: new[] { "AuthorId", "BookId" },
                keyValues: new object[] { 3, 3 },
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 1,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 2,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "CategoryId",
                keyValue: 3,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "LibrarySettings",
                keyColumn: "LibrarySettingId",
                keyValue: 1,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "MembershipTypes",
                keyColumn: "MembershipTypeId",
                keyValue: 1,
                column: "DeletedAt",
                value: null);

            migrationBuilder.UpdateData(
                table: "MembershipTypes",
                keyColumn: "MembershipTypeId",
                keyValue: 2,
                column: "DeletedAt",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookId_MemberId",
                table: "Reservations",
                columns: new[] { "BookId", "MemberId" },
                unique: true,
                filter: "\"Status\" = 'Pending' AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookId_ReservedAt_ReservationId",
                table: "Reservations",
                columns: new[] { "BookId", "ReservedAt", "ReservationId" },
                filter: "\"Status\" = 'Pending' AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipTypes_Name",
                table: "MembershipTypes",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BookId_MemberId",
                table: "Loans",
                columns: new[] { "BookId", "MemberId" },
                unique: true,
                filter: "\"ReturnDate\" IS NULL AND \"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Books_ISBN",
                table: "Books",
                column: "ISBN",
                unique: true,
                filter: "\"DeletedAt\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_BookId_MemberId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_BookId_ReservedAt_ReservationId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_MembershipTypes_Name",
                table: "MembershipTypes");

            migrationBuilder.DropIndex(
                name: "IX_Loans_BookId_MemberId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Name",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Books_ISBN",
                table: "Books");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "MembershipTypes");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Memberships");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "LibrarySettings");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "BookAuthors");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Authors");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookId_MemberId",
                table: "Reservations",
                columns: new[] { "BookId", "MemberId" },
                unique: true,
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_BookId_ReservedAt_ReservationId",
                table: "Reservations",
                columns: new[] { "BookId", "ReservedAt", "ReservationId" },
                filter: "\"Status\" = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_MembershipTypes_Name",
                table: "MembershipTypes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_BookId_MemberId",
                table: "Loans",
                columns: new[] { "BookId", "MemberId" },
                unique: true,
                filter: "\"ReturnDate\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Name",
                table: "Categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Books_ISBN",
                table: "Books",
                column: "ISBN",
                unique: true);
        }
    }
}
