using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionRoundsAndJudgeAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Round",
                table: "Submissions");

            migrationBuilder.AddColumn<int>(
                name: "CompetitionRoundId",
                table: "Submissions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetitionRounds",
                columns: table => new
                {
                    RoundId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetitionId = table.Column<int>(type: "integer", nullable: false),
                    RoundName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RoundOrder = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SubmissionDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    MaxAdvancing = table.Column<int>(type: "integer", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionRounds", x => x.RoundId);
                    table.ForeignKey(
                        name: "FK_CompetitionRounds_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "CompetitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JudgeAssignments",
                columns: table => new
                {
                    AssignmentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    JudgeId = table.Column<int>(type: "integer", nullable: false),
                    SubmissionId = table.Column<int>(type: "integer", nullable: false),
                    CompetitionId = table.Column<int>(type: "integer", nullable: false),
                    RoundId = table.Column<int>(type: "integer", nullable: true),
                    AssignedByUserId = table.Column<int>(type: "integer", nullable: false),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GradingDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JudgeAssignments", x => x.AssignmentId);
                    table.ForeignKey(
                        name: "FK_JudgeAssignments_CompetitionRounds_RoundId",
                        column: x => x.RoundId,
                        principalTable: "CompetitionRounds",
                        principalColumn: "RoundId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_JudgeAssignments_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "CompetitionId");
                    table.ForeignKey(
                        name: "FK_JudgeAssignments_Judges_JudgeId",
                        column: x => x.JudgeId,
                        principalTable: "Judges",
                        principalColumn: "JudgeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JudgeAssignments_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "SubmissionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_JudgeAssignments_Users_AssignedByUserId",
                        column: x => x.AssignedByUserId,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_CompetitionRoundId",
                table: "Submissions",
                column: "CompetitionRoundId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionRounds_CompetitionId",
                table: "CompetitionRounds",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeAssignments_AssignedByUserId",
                table: "JudgeAssignments",
                column: "AssignedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeAssignments_CompetitionId",
                table: "JudgeAssignments",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeAssignments_JudgeId_SubmissionId",
                table: "JudgeAssignments",
                columns: new[] { "JudgeId", "SubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JudgeAssignments_RoundId",
                table: "JudgeAssignments",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_JudgeAssignments_SubmissionId",
                table: "JudgeAssignments",
                column: "SubmissionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Submissions_CompetitionRounds_CompetitionRoundId",
                table: "Submissions",
                column: "CompetitionRoundId",
                principalTable: "CompetitionRounds",
                principalColumn: "RoundId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Submissions_CompetitionRounds_CompetitionRoundId",
                table: "Submissions");

            migrationBuilder.DropTable(
                name: "JudgeAssignments");

            migrationBuilder.DropTable(
                name: "CompetitionRounds");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_CompetitionRoundId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "CompetitionRoundId",
                table: "Submissions");

            migrationBuilder.AddColumn<int>(
                name: "Round",
                table: "Submissions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 2, 20, 26, 524, DateTimeKind.Utc).AddTicks(9443));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 2, 20, 26, 524, DateTimeKind.Utc).AddTicks(9446));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 2, 20, 26, 524, DateTimeKind.Utc).AddTicks(9448));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 2, 20, 26, 524, DateTimeKind.Utc).AddTicks(9449));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 4, 2, 20, 26, 524, DateTimeKind.Utc).AddTicks(9450));
        }
    }
}
