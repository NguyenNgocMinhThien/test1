using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Web_cham_diem.Migrations
{
    /// <inheritdoc />
    public partial class AddCompetitionImagesDocumentsAndRegistrationRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoundId",
                table: "Registrations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompetitionDocuments",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetitionId = table.Column<int>(type: "integer", nullable: false),
                    FileName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FileUrl = table.Column<string>(type: "text", nullable: false),
                    FileType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionDocuments", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_CompetitionDocuments_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "CompetitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompetitionImages",
                columns: table => new
                {
                    ImageId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetitionId = table.Column<int>(type: "integer", nullable: false),
                    ImageUrl = table.Column<string>(type: "text", nullable: false),
                    IsThumbnail = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompetitionImages", x => x.ImageId);
                    table.ForeignKey(
                        name: "FK_CompetitionImages_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "CompetitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RegistrationRounds",
                columns: table => new
                {
                    RoundId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompetitionId = table.Column<int>(type: "integer", nullable: false),
                    RoundName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrationRounds", x => x.RoundId);
                    table.ForeignKey(
                        name: "FK_RegistrationRounds_Competitions_CompetitionId",
                        column: x => x.CompetitionId,
                        principalTable: "Competitions",
                        principalColumn: "CompetitionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 2, 4, 3, 38, 566, DateTimeKind.Utc).AddTicks(2638));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 2, 4, 3, 38, 566, DateTimeKind.Utc).AddTicks(2641));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 2, 4, 3, 38, 566, DateTimeKind.Utc).AddTicks(2643));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 2, 4, 3, 38, 566, DateTimeKind.Utc).AddTicks(2645));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 6, 2, 4, 3, 38, 566, DateTimeKind.Utc).AddTicks(2647));

            migrationBuilder.CreateIndex(
                name: "IX_Registrations_RoundId",
                table: "Registrations",
                column: "RoundId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionDocuments_CompetitionId",
                table: "CompetitionDocuments",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_CompetitionImages_CompetitionId",
                table: "CompetitionImages",
                column: "CompetitionId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrationRounds_CompetitionId",
                table: "RegistrationRounds",
                column: "CompetitionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Registrations_RegistrationRounds_RoundId",
                table: "Registrations",
                column: "RoundId",
                principalTable: "RegistrationRounds",
                principalColumn: "RoundId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Registrations_RegistrationRounds_RoundId",
                table: "Registrations");

            migrationBuilder.DropTable(
                name: "CompetitionDocuments");

            migrationBuilder.DropTable(
                name: "CompetitionImages");

            migrationBuilder.DropTable(
                name: "RegistrationRounds");

            migrationBuilder.DropIndex(
                name: "IX_Registrations_RoundId",
                table: "Registrations");

            migrationBuilder.DropColumn(
                name: "RoundId",
                table: "Registrations");

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 15, 16, 19, 378, DateTimeKind.Utc).AddTicks(1949));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 15, 16, 19, 378, DateTimeKind.Utc).AddTicks(1951));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 15, 16, 19, 378, DateTimeKind.Utc).AddTicks(1953));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 15, 16, 19, 378, DateTimeKind.Utc).AddTicks(1955));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 5, 26, 15, 16, 19, 378, DateTimeKind.Utc).AddTicks(1956));
        }
    }
}
