using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEventRemindersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventReminders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EventId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ReminderType = table.Column<int>(type: "INTEGER", nullable: false),
                    CustomTimeHours = table.Column<int>(type: "INTEGER", nullable: true),
                    CustomTimeMinutes = table.Column<int>(type: "INTEGER", nullable: true),
                    TriggerDateTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsSent = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventReminders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventReminders_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventReminders_EventId",
                table: "EventReminders",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_EventReminders_IsSent",
                table: "EventReminders",
                column: "IsSent");

            migrationBuilder.CreateIndex(
                name: "IX_EventReminders_TriggerDateTime",
                table: "EventReminders",
                column: "TriggerDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_EventReminders_TriggerDateTime_IsSent",
                table: "EventReminders",
                columns: new[] { "TriggerDateTime", "IsSent" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventReminders");
        }
    }
}
