using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamMemberList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MemberList",
                table: "Teams",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5079));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5082));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5083));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5084));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5086));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberList",
                table: "Teams");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 9, 1, 22, 646, DateTimeKind.Utc).AddTicks(5143));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 9, 1, 22, 646, DateTimeKind.Utc).AddTicks(5146));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 9, 1, 22, 646, DateTimeKind.Utc).AddTicks(5148));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 9, 1, 22, 646, DateTimeKind.Utc).AddTicks(5150));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 5, 9, 1, 22, 646, DateTimeKind.Utc).AddTicks(5151));
        }
    }
}
