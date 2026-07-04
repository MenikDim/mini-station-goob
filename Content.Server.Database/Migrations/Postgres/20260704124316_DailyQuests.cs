using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
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
                    daily_quest_progress_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quest_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    assigned_quest_ids = table.Column<string>(type: "text", nullable: false),
                    progress_values = table.Column<string>(type: "text", nullable: false),
                    status_flags = table.Column<string>(type: "text", nullable: false)
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
