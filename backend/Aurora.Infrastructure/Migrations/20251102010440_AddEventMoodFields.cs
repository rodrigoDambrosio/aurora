using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventMoodFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Las columnas MoodRating y MoodNotes ya existen en la BD
            // Esta migración se marca como aplicada sin ejecutar cambios
            migrationBuilder.Sql("SELECT 1;"); // No-op
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MoodRating",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "MoodNotes",
                table: "Events");
        }
    }
}
