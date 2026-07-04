using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class DailyQuests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "daily_quest_progress",
                columns: table => new
                {
                    daily_quest_progress_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    quest_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    assigned_quest_ids = table.Column<string>(type: "TEXT", nullable: false),
                    progress_values = table.Column<string>(type: "TEXT", nullable: false),
                    status_flags = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_daily_quest_progress", x => x.daily_quest_progress_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_daily_quest_progress_player_id_quest_date",
                table: "daily_quest_progress",
                columns: new[] { "player_id", "quest_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "daily_quest_progress");
        }
    }
}
