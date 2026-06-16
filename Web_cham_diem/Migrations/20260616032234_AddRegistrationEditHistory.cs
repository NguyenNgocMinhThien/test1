using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationEditHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrationEditHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RegistrationId = table.Column<int>(type: "integer", nullable: false),
                    EditedBy = table.Column<int>(type: "integer", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ChangesSummary = table.Column<string>(type: "text", nullable: true),
                    PreviousStatus = table.Column<string>(type: "text", nullable: true),
                    NewStatus = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationEditHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_RegistrationEditHistories_Registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalTable: "Registrations",
                        principalColumn: "RegistrationId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrationEditHistories_Users_EditedBy",
                        column: x => x.EditedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEditHistories_EditedBy",
                table: "RegistrationEditHistories",
                column: "EditedBy");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationEditHistories_RegistrationId",
                table: "RegistrationEditHistories",
                column: "RegistrationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrationEditHistories");

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
        }
    }
}
