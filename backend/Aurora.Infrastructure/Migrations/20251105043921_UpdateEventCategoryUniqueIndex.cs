using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEventCategoryUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Los cambios de índices y la tabla RecommendationFeedback ya existen
            // Esta migración se marca como aplicada sin ejecutar cambios
            migrationBuilder.Sql("SELECT 1;"); // No-op

            // Código original comentado:
            /*
            migrationBuilder.DropIndex(
                name: "IX_EventCategories_UserId_Name",
                table: "EventCategories");

            migrationBuilder.DropIndex(
                name: "IX_DailyMoodEntries_UserId",
                table: "DailyMoodEntries");

            migrationBuilder.CreateTable(
                name: "RecommendationFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RecommendationId = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    Accepted = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    MoodAfter = table.Column<int>(type: "INTEGER", nullable: true),
                    SubmittedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "datetime('now')"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecommendationFeedback_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_UserId_Name_IsActive",
                table: "EventCategories",
                columns: new[] { "UserId", "Name", "IsActive" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RecommendationFeedback_UserId_RecommendationId",
                table: "RecommendationFeedback",
                columns: new[] { "UserId", "RecommendationId" },
                unique: true);
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecommendationFeedback");

            migrationBuilder.DropIndex(
                name: "IX_EventCategories_UserId_Name_IsActive",
                table: "EventCategories");

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_UserId_Name",
                table: "EventCategories",
                columns: new[] { "UserId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DailyMoodEntries_UserId",
                table: "DailyMoodEntries",
                column: "UserId");
        }
    }
}
