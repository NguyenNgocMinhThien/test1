using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundIdToJudges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Judges_UserId",
                table: "Judges");

            migrationBuilder.AddColumn<int>(
                name: "RoundId",
                table: "Judges",
                type: "integer",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 30, 57, 23, DateTimeKind.Utc).AddTicks(207));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 30, 57, 23, DateTimeKind.Utc).AddTicks(210));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 30, 57, 23, DateTimeKind.Utc).AddTicks(212));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 30, 57, 23, DateTimeKind.Utc).AddTicks(213));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 30, 57, 23, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.CreateIndex(
                name: "IX_Judges_RoundId",
                table: "Judges",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_Judges_UserId_CompetitionId_RoundId",
                table: "Judges",
                columns: new[] { "UserId", "CompetitionId", "RoundId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Judges_CompetitionRounds_RoundId",
                table: "Judges",
                column: "RoundId",
                principalTable: "CompetitionRounds",
                principalColumn: "RoundId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Judges_CompetitionRounds_RoundId",
                table: "Judges");

            migrationBuilder.DropIndex(
                name: "IX_Judges_RoundId",
                table: "Judges");

            migrationBuilder.DropIndex(
                name: "IX_Judges_UserId_CompetitionId_RoundId",
                table: "Judges");

            migrationBuilder.DropColumn(
                name: "RoundId",
                table: "Judges");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 10, 34, 700, DateTimeKind.Utc).AddTicks(6555));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 10, 34, 700, DateTimeKind.Utc).AddTicks(6558));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 10, 34, 700, DateTimeKind.Utc).AddTicks(6559));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 10, 34, 700, DateTimeKind.Utc).AddTicks(6561));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 10, 34, 700, DateTimeKind.Utc).AddTicks(6563));

            migrationBuilder.CreateIndex(
                name: "IX_Judges_UserId",
                table: "Judges",
                column: "UserId");
        }
    }
}
