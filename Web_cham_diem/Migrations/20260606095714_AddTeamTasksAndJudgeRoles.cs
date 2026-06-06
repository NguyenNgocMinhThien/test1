using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamTasksAndJudgeRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MemberList",
                table: "Teams");

            migrationBuilder.AddColumn<string>(
                name: "ApprovalStatus",
                table: "Scores",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "Scores",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedBy",
                table: "Scores",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "Scores",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JudgeRole",
                table: "Judges",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                columns: table => new
                {
                    TeamMemberId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    InvitedBy = table.Column<int>(type: "integer", nullable: true),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.TeamMemberId);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_InvitedBy",
                        column: x => x.InvitedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_TeamMembers_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeamTasks",
                columns: table => new
                {
                    TaskId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TeamId = table.Column<int>(type: "integer", nullable: false),
                    AssignedBy = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamTasks", x => x.TaskId);
                    table.ForeignKey(
                        name: "FK_TeamTasks_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "TeamId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TeamTasks_Users_AssignedBy",
                        column: x => x.AssignedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TaskCompletions",
                columns: table => new
                {
                    CompletionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaskId = table.Column<int>(type: "integer", nullable: false),
                    CompletedBy = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "text", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    VerifiedBy = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskCompletions", x => x.CompletionId);
                    table.ForeignKey(
                        name: "FK_TaskCompletions_TeamTasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "TeamTasks",
                        principalColumn: "TaskId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskCompletions_Users_CompletedBy",
                        column: x => x.CompletedBy,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TaskCompletions_Users_VerifiedBy",
                        column: x => x.VerifiedBy,
                        principalTable: "Users",
                        principalColumn: "UserId");
                });

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

            migrationBuilder.CreateIndex(
                name: "IX_Scores_ApprovedBy",
                table: "Scores",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletions_CompletedBy",
                table: "TaskCompletions",
                column: "CompletedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletions_TaskId_CompletedBy",
                table: "TaskCompletions",
                columns: new[] { "TaskId", "CompletedBy" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaskCompletions_VerifiedBy",
                table: "TaskCompletions",
                column: "VerifiedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_InvitedBy",
                table: "TeamMembers",
                column: "InvitedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_UserId",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_UserId",
                table: "TeamMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTasks_AssignedBy",
                table: "TeamTasks",
                column: "AssignedBy");

            migrationBuilder.CreateIndex(
                name: "IX_TeamTasks_TeamId",
                table: "TeamTasks",
                column: "TeamId");

            migrationBuilder.AddForeignKey(
                name: "FK_Scores_Users_ApprovedBy",
                table: "Scores",
                column: "ApprovedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Scores_Users_ApprovedBy",
                table: "Scores");

            migrationBuilder.DropTable(
                name: "TaskCompletions");

            migrationBuilder.DropTable(
                name: "TeamMembers");

            migrationBuilder.DropTable(
                name: "TeamTasks");

            migrationBuilder.DropIndex(
                name: "IX_Scores_ApprovedBy",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "ApprovedBy",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "Scores");

            migrationBuilder.DropColumn(
                name: "JudgeRole",
                table: "Judges");

            migrationBuilder.AddColumn<string>(
                name: "MemberList",
                table: "Teams",
                type: "text",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5079));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5082));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5083));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5084));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 6, 6, 3, 40, 533, DateTimeKind.Utc).AddTicks(5086));
        }
    }
}
