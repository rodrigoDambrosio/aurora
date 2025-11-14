using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddScheduleSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // La tabla ScheduleSuggestions ya existe en la BD
            // Esta migración se marca como aplicada sin ejecutar cambios
            migrationBuilder.Sql("SELECT 1;"); // No-op

            // Código original comentado:
            /*
            // Intenta eliminar índices antiguos solo si existen
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_EventCategories_UserId_Name;
                DROP INDEX IF EXISTS IX_EventCategories_UserId_Name_IsActive;
            ");

            migrationBuilder.CreateTable(
                name: "ScheduleSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    SuggestedDateTime = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    RespondedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ConfidenceScore = table.Column<int>(type: "INTEGER", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleSuggestions_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventCategories_UserId_Name_Active",
                table: "EventCategories",
                columns: new[] { "UserId", "Name" },
                unique: true,
                filter: "IsActive = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleSuggestions_EventId",
                table: "ScheduleSuggestions",
                column: "EventId");
            */
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ScheduleSuggestions");

            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS IX_EventCategories_UserId_Name_Active;
            ");

            // Recrea índices antiguos solo si no existen
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_EventCategories_UserId_Name 
                ON EventCategories (UserId, Name);
                
                CREATE UNIQUE INDEX IF NOT EXISTS IX_EventCategories_UserId_Name_IsActive 
                ON EventCategories (UserId, Name, IsActive);
            ");
        }
    }
}
