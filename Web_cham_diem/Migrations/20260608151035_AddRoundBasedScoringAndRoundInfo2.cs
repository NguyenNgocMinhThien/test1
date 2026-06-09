using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddRoundBasedScoringAndRoundInfo2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoringCriteria_Competitions_CompetitionId",
                table: "ScoringCriteria");

            migrationBuilder.RenameColumn(
                name: "CompetitionId",
                table: "ScoringCriteria",
                newName: "RoundId");

            migrationBuilder.RenameIndex(
                name: "IX_ScoringCriteria_CompetitionId",
                table: "ScoringCriteria",
                newName: "IX_ScoringCriteria_RoundId");

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "CompetitionRounds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "CompetitionRounds",
                type: "character varying(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "CompetitionRounds",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MeetingTime",
                table: "CompetitionRounds",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresFile",
                table: "CompetitionRounds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresOtherLink",
                table: "CompetitionRounds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresVideo",
                table: "CompetitionRounds",
                type: "boolean",
                nullable: false,
                defaultValue: false);

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

            migrationBuilder.AddForeignKey(
                name: "FK_ScoringCriteria_CompetitionRounds_RoundId",
                table: "ScoringCriteria",
                column: "RoundId",
                principalTable: "CompetitionRounds",
                principalColumn: "RoundId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScoringCriteria_CompetitionRounds_RoundId",
                table: "ScoringCriteria");

            migrationBuilder.RenameColumn(
                name: "RoundId",
                table: "ScoringCriteria",
                newName: "CompetitionId");

            migrationBuilder.RenameIndex(
                name: "IX_ScoringCriteria_RoundId",
                table: "ScoringCriteria",
                newName: "IX_ScoringCriteria_CompetitionId");

            migrationBuilder.DropColumn(name: "IsOnline",        table: "CompetitionRounds");
            migrationBuilder.DropColumn(name: "Location",        table: "CompetitionRounds");
            migrationBuilder.DropColumn(name: "MeetingLink",     table: "CompetitionRounds");
            migrationBuilder.DropColumn(name: "MeetingTime",     table: "CompetitionRounds");
            migrationBuilder.DropColumn(name: "RequiresFile",    table: "CompetitionRounds");
            migrationBuilder.DropColumn(name: "RequiresOtherLink", table: "CompetitionRounds");
            migrationBuilder.DropColumn(name: "RequiresVideo",   table: "CompetitionRounds");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 5, 49, 265, DateTimeKind.Utc).AddTicks(9321));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 5, 49, 265, DateTimeKind.Utc).AddTicks(9323));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 5, 49, 265, DateTimeKind.Utc).AddTicks(9325));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 5, 49, 265, DateTimeKind.Utc).AddTicks(9326));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 8, 15, 5, 49, 265, DateTimeKind.Utc).AddTicks(9328));

            migrationBuilder.AddForeignKey(
                name: "FK_ScoringCriteria_Competitions_CompetitionId",
                table: "ScoringCriteria",
                column: "CompetitionId",
                principalTable: "Competitions",
                principalColumn: "CompetitionId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
