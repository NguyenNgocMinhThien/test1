using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class FinalMigrations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AssignedTo",
                table: "TeamTasks",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 15, 24, 22, 495, DateTimeKind.Utc).AddTicks(1659));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 15, 24, 22, 495, DateTimeKind.Utc).AddTicks(1660));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 15, 24, 22, 495, DateTimeKind.Utc).AddTicks(1662));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 15, 24, 22, 495, DateTimeKind.Utc).AddTicks(1663));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 15, 24, 22, 495, DateTimeKind.Utc).AddTicks(1664));

            migrationBuilder.CreateIndex(
                name: "IX_TeamTasks_AssignedTo",
                table: "TeamTasks",
                column: "AssignedTo");

            migrationBuilder.AddForeignKey(
                name: "FK_TeamTasks_Users_AssignedTo",
                table: "TeamTasks",
                column: "AssignedTo",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TeamTasks_Users_AssignedTo",
                table: "TeamTasks");

            migrationBuilder.DropIndex(
                name: "IX_TeamTasks_AssignedTo",
                table: "TeamTasks");

            migrationBuilder.DropColumn(
                name: "AssignedTo",
                table: "TeamTasks");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 11, 40, 20, 144, DateTimeKind.Utc).AddTicks(6099));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 11, 40, 20, 144, DateTimeKind.Utc).AddTicks(6100));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 11, 40, 20, 144, DateTimeKind.Utc).AddTicks(6102));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 11, 40, 20, 144, DateTimeKind.Utc).AddTicks(6103));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 11, 40, 20, 144, DateTimeKind.Utc).AddTicks(6104));
        }
    }
}
