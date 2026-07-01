using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AdminHelpRating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "admin_help_rating",
                columns: table => new
                {
                    admin_help_rating_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    player_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    admin_user_id = table.Column<Guid>(type: "TEXT", nullable: false),
                    round_id = table.Column<int>(type: "INTEGER", nullable: true),
                    stars = table.Column<byte>(type: "INTEGER", nullable: false),
                    created_at = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_admin_help_rating", x => x.admin_help_rating_id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_admin_help_rating_player_user_id_created_at",
                table: "admin_help_rating",
                columns: new[] { "player_user_id", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "admin_help_rating");
        }
    }
}
