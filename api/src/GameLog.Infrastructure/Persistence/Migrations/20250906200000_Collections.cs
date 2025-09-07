using GameLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLog.Infrastructure.Persistence.Migrations;

/// <summary>User-curated ordered game collections.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20250906200000_Collections")]
public class Collections : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GameCollections",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                CoverKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Visibility = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameCollections", x => x.Id);
                table.ForeignKey(name: "FK_GameCollections_Users_UserId", column: x => x.UserId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CollectionGames",
            columns: table => new
            {
                CollectionId = table.Column<Guid>(type: "uuid", nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: false),
                Position = table.Column<int>(type: "integer", nullable: false),
                AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CollectionGames", x => new { x.CollectionId, x.GameId });
                table.ForeignKey(name: "FK_CollectionGames_GameCollections_CollectionId", column: x => x.CollectionId, principalTable: "GameCollections", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_CollectionGames_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_GameCollections_UserId_CreatedAt", "GameCollections", new[] { "UserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_CollectionGames_CollectionId_Position", "CollectionGames", new[] { "CollectionId", "Position" }, unique: true);
        migrationBuilder.CreateIndex("IX_CollectionGames_GameId", "CollectionGames", "GameId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CollectionGames");
        migrationBuilder.DropTable("GameCollections");
    }
}

