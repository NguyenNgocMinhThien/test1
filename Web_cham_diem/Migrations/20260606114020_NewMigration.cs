using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class NewMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdvisorId",
                table: "Registrations",
                type: "integer",
                nullable: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_AdvisorId",
                table: "Registrations",
                column: "AdvisorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_Users_AdvisorId",
                table: "Registrations",
                column: "AdvisorId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_Users_AdvisorId",
                table: "Registrations");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_AdvisorId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "AdvisorId",
                table: "Registrations");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 9, 57, 12, 528, DateTimeKind.Utc).AddTicks(5818));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 9, 57, 12, 528, DateTimeKind.Utc).AddTicks(5820));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 9, 57, 12, 528, DateTimeKind.Utc).AddTicks(5821));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 9, 57, 12, 528, DateTimeKind.Utc).AddTicks(5822));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 9, 57, 12, 528, DateTimeKind.Utc).AddTicks(5824));
        }
    }
}
