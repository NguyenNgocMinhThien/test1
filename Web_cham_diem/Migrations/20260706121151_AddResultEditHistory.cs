using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddResultEditHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ResultEditHistories",
                columns: table => new
                {
                    HistoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetitionId = table.Column<int>(type: "integer", nullable: false),
                    RoundId = table.Column<int>(type: "integer", nullable: true),
                    SubmissionId = table.Column<int>(type: "integer", nullable: true),
                    JudgeId = table.Column<int>(type: "integer", nullable: true),
                    CriteriaId = table.Column<int>(type: "integer", nullable: true),
                    EditedBy = table.Column<int>(type: "integer", nullable: false),
                    EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ChangesSummary = table.Column<string>(type: "text", nullable: false),
                    OldValue = table.Column<string>(type: "text", nullable: true),
                    NewValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResultEditHistories", x => x.HistoryId);
                    table.ForeignKey(
                        name: "FK_ResultEditHistories_CompetitionRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "CompetitionRounds",
                        principalColumn: "RoundId");
                    table.ForeignKey(
                        name: "FK_ResultEditHistories_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "CompetitionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ResultEditHistories_Judges_JudgeId",
                        column: x => x.JudgeId,
                        principalTable: "Judges",
                        principalColumn: "JudgeId");
                    table.ForeignKey(
                        name: "FK_ResultEditHistories_ScoringCriteria_CriteriaId",
                        column: x => x.CriteriaId,
                        principalTable: "ScoringCriteria",
                        principalColumn: "CriteriaId");
                    table.ForeignKey(
                        name: "FK_ResultEditHistories_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId");
                    table.ForeignKey(
                        name: "FK_ResultEditHistories_Users_EditedBy",
                        column: x => x.EditedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 6, 12, 11, 49, 312, DateTimeKind.Utc).AddTicks(6281));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 6, 12, 11, 49, 312, DateTimeKind.Utc).AddTicks(6284));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 6, 12, 11, 49, 312, DateTimeKind.Utc).AddTicks(6286));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 6, 12, 11, 49, 312, DateTimeKind.Utc).AddTicks(6287));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 7, 6, 12, 11, 49, 312, DateTimeKind.Utc).AddTicks(6289));

            migrationBuilder.CreateIndex(
                name: "IX_ResultEditHistories_CompetitionId",
                table: "ResultEditHistories",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultEditHistories_CriteriaId",
                table: "ResultEditHistories",
                column: "CriteriaId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultEditHistories_EditedBy",
                table: "ResultEditHistories",
                column: "EditedBy");

            migrationBuilder.CreateIndex(
                name: "IX_ResultEditHistories_JudgeId",
                table: "ResultEditHistories",
                column: "JudgeId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultEditHistories_RoundId",
                table: "ResultEditHistories",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_ResultEditHistories_SubmissionId",
                table: "ResultEditHistories",
                column: "SubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResultEditHistories");

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
        }
    }
}
