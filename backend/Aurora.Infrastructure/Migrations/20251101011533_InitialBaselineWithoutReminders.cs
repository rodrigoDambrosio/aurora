using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Aurora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialBaselineWithoutReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Baseline vacía - las tablas existentes ya están en la BD
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No hay nada que hacer en Down
        }
    }
}
