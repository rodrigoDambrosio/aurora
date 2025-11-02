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
            migrationBuilder.AddColumn<int>(
                name: "MoodRating",
                table: "Events",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MoodNotes",
                table: "Events",
                type: "TEXT",
                nullable: true);
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
