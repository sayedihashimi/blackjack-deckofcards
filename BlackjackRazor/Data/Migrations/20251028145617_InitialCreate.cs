using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BlackjackRazor.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Username = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: false),
                    DeckCount = table.Column<int>(type: "INTEGER", nullable: false),
                    DeckId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    StartedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    EndedUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    DefaultBet = table.Column<int>(type: "INTEGER", nullable: false),
                    BankrollStart = table.Column<decimal>(type: "TEXT", nullable: false),
                    BankrollEnd = table.Column<decimal>(type: "TEXT", nullable: false),
                    HandsPlayed = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerBlackjacks = table.Column<int>(type: "INTEGER", nullable: false),
                    DealerBlackjacks = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerBusts = table.Column<int>(type: "INTEGER", nullable: false),
                    DealerBusts = table.Column<int>(type: "INTEGER", nullable: false),
                    PlayerWins = table.Column<int>(type: "INTEGER", nullable: false),
                    DealerWins = table.Column<int>(type: "INTEGER", nullable: false),
                    Pushes = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Games_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Hands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GameId = table.Column<int>(type: "INTEGER", nullable: false),
                    HandIndex = table.Column<int>(type: "INTEGER", nullable: false),
                    Bet = table.Column<decimal>(type: "TEXT", nullable: false),
                    Outcome = table.Column<int>(type: "INTEGER", nullable: false),
                    Payout = table.Column<decimal>(type: "TEXT", nullable: false),
                    WasSplit = table.Column<bool>(type: "INTEGER", nullable: false),
                    WasDouble = table.Column<bool>(type: "INTEGER", nullable: false),
                    PlayedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Hands_Games_GameId",
                        column: x => x.GameId,
                        principalTable: "Games",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_UserId",
                table: "Games",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Hands_GameId_HandIndex",
                table: "Hands",
                columns: new[] { "GameId", "HandIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_Username",
                table: "Users",
                column: "Username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Hands");

            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
