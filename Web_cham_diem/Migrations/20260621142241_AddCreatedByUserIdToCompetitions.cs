using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedByUserIdToCompetitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Competitions",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 21, 14, 22, 39, 302, DateTimeKind.Utc).AddTicks(7002));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 21, 14, 22, 39, 302, DateTimeKind.Utc).AddTicks(7005));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 21, 14, 22, 39, 302, DateTimeKind.Utc).AddTicks(7007));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 21, 14, 22, 39, 302, DateTimeKind.Utc).AddTicks(7009));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 21, 14, 22, 39, 302, DateTimeKind.Utc).AddTicks(7011));

            migrationBuilder.CreateIndex(
                name: "IX_Competitions_CreatedByUserId",
                table: "Competitions",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Competitions_Users_CreatedByUserId",
                table: "Competitions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Competitions_Users_CreatedByUserId",
                table: "Competitions");

            migrationBuilder.DropIndex(
                name: "IX_Competitions_CreatedByUserId",
                table: "Competitions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Competitions");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 4, 36, 40, 950, DateTimeKind.Utc).AddTicks(8017));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 4, 36, 40, 950, DateTimeKind.Utc).AddTicks(8019));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 4, 36, 40, 950, DateTimeKind.Utc).AddTicks(8022));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 4, 36, 40, 950, DateTimeKind.Utc).AddTicks(8023));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 4, 36, 40, 950, DateTimeKind.Utc).AddTicks(8025));
        }
    }
}
