using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionActorTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "Submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 4, 4, 10, 7, 420, DateTimeKind.Utc).AddTicks(5995));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 4, 4, 10, 7, 420, DateTimeKind.Utc).AddTicks(5997));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 4, 4, 10, 7, 420, DateTimeKind.Utc).AddTicks(5998));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 4, 4, 10, 7, 420, DateTimeKind.Utc).AddTicks(6000));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 4, 4, 10, 7, 420, DateTimeKind.Utc).AddTicks(6001));

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CreatedByUserId",
                table: "Submissions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_UpdatedByUserId",
                table: "Submissions",
                column: "UpdatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_CreatedByUserId",
                table: "Submissions",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_Users_UpdatedByUserId",
                table: "Submissions",
                column: "UpdatedByUserId",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_CreatedByUserId",
                table: "Submissions");

            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_Users_UpdatedByUserId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_CreatedByUserId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_UpdatedByUserId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Submissions");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 2, 8, 47, 30, 980, DateTimeKind.Utc).AddTicks(9939));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 2, 8, 47, 30, 980, DateTimeKind.Utc).AddTicks(9941));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 2, 8, 47, 30, 980, DateTimeKind.Utc).AddTicks(9943));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 2, 8, 47, 30, 980, DateTimeKind.Utc).AddTicks(9945));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 2, 8, 47, 30, 980, DateTimeKind.Utc).AddTicks(9947));
        }
    }
}
