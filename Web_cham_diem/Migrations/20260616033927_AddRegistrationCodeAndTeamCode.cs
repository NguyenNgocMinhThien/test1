using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationCodeAndTeamCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TeamCode",
                table: "Teams",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationCode",
                table: "Registrations",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 39, 26, 997, DateTimeKind.Utc).AddTicks(3148));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 39, 26, 997, DateTimeKind.Utc).AddTicks(3150));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 39, 26, 997, DateTimeKind.Utc).AddTicks(3152));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 39, 26, 997, DateTimeKind.Utc).AddTicks(3154));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 39, 26, 997, DateTimeKind.Utc).AddTicks(3156));

            migrationBuilder.CreateIndex(
                name: "IX_Teams_TeamCode",
                table: "Teams",
                column: "TeamCode",
                unique: true,
                filter: "\"TeamCode\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_RegistrationCode",
                table: "Registrations",
                column: "RegistrationCode",
                unique: true,
                filter: "\"RegistrationCode\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Teams_TeamCode",
                table: "Teams");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_RegistrationCode",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "TeamCode",
                table: "Teams");

            migrationBuilder.DropColumn(
                name: "RegistrationCode",
                table: "Registrations");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 22, 32, 377, DateTimeKind.Utc).AddTicks(2030));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 22, 32, 377, DateTimeKind.Utc).AddTicks(2033));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 22, 32, 377, DateTimeKind.Utc).AddTicks(2035));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 22, 32, 377, DateTimeKind.Utc).AddTicks(2036));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 16, 3, 22, 32, 377, DateTimeKind.Utc).AddTicks(2038));
        }
    }
}
