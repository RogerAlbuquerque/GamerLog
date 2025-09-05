using GameLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLog.Infrastructure.Persistence.Migrations;

/// <summary>Gaming diary, playthroughs, ratings and reviews.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20250905120000_DiaryRatingsReviews")]
public class DiaryRatingsReviews : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "GamingDiaryEntries",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: false),
                Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                PlatformId = table.Column<int>(type: "integer", nullable: true),
                TotalPlaytimeMinutes = table.Column<int>(type: "integer", nullable: false),
                Recommended = table.Column<bool>(type: "boolean", nullable: true),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GamingDiaryEntries", x => x.Id);
                table.ForeignKey(name: "FK_GamingDiaryEntries_Users_UserId", column: x => x.UserId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_GamingDiaryEntries_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_GamingDiaryEntries_Platforms_PlatformId", column: x => x.PlatformId, principalTable: "Platforms", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "Playthroughs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                DiaryEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                Number = table.Column<int>(type: "integer", nullable: false),
                StartedOn = table.Column<DateOnly>(type: "date", nullable: true),
                CompletedOn = table.Column<DateOnly>(type: "date", nullable: true),
                PlaytimeMinutes = table.Column<int>(type: "integer", nullable: false),
                PlatformId = table.Column<int>(type: "integer", nullable: true),
                Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Playthroughs", x => x.Id);
                table.ForeignKey(name: "FK_Playthroughs_GamingDiaryEntries_DiaryEntryId", column: x => x.DiaryEntryId, principalTable: "GamingDiaryEntries", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Playthroughs_Platforms_PlatformId", column: x => x.PlatformId, principalTable: "Platforms", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
                table.CheckConstraint("CK_Playthroughs_Dates", "\"CompletedOn\" IS NULL OR \"StartedOn\" IS NULL OR \"CompletedOn\" >= \"StartedOn\"");
            });

        migrationBuilder.CreateTable(
            name: "Ratings",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: false),
                HalfStars = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Ratings", x => x.Id);
                table.ForeignKey(name: "FK_Ratings_Users_UserId", column: x => x.UserId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Ratings_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_Ratings_HalfStars", "\"HalfStars\" BETWEEN 1 AND 10");
            });

        migrationBuilder.CreateTable(
            name: "Reviews",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: false),
                Title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                Body = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                Experience = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: true),
                Pros = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                Cons = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                FavoriteCharacter = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                FavoriteMoment = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                Difficulty = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                PlaytimeMinutes = table.Column<int>(type: "integer", nullable: true),
                PlatformId = table.Column<int>(type: "integer", nullable: true),
                CompletedOn = table.Column<DateOnly>(type: "date", nullable: true),
                ContainsSpoilers = table.Column<bool>(type: "boolean", nullable: false),
                LikeCount = table.Column<int>(type: "integer", nullable: false),
                CommentCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Reviews", x => x.Id);
                table.ForeignKey(name: "FK_Reviews_Users_UserId", column: x => x.UserId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Reviews_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Reviews_Platforms_PlatformId", column: x => x.PlatformId, principalTable: "Platforms", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ReviewLikes",
            columns: table => new
            {
                ReviewId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ReviewLikes", x => new { x.ReviewId, x.UserId });
                table.ForeignKey(name: "FK_ReviewLikes_Reviews_ReviewId", column: x => x.ReviewId, principalTable: "Reviews", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_ReviewLikes_Users_UserId", column: x => x.UserId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_GamingDiaryEntries_UserId_GameId", "GamingDiaryEntries", new[] { "UserId", "GameId" }, unique: true);
        migrationBuilder.CreateIndex("IX_GamingDiaryEntries_UserId_Status", "GamingDiaryEntries", new[] { "UserId", "Status" });
        migrationBuilder.CreateIndex("IX_GamingDiaryEntries_GameId_Status", "GamingDiaryEntries", new[] { "GameId", "Status" });
        migrationBuilder.CreateIndex("IX_Playthroughs_DiaryEntryId_Number", "Playthroughs", new[] { "DiaryEntryId", "Number" }, unique: true);
        migrationBuilder.CreateIndex("IX_Playthroughs_CompletedOn", "Playthroughs", "CompletedOn");
        migrationBuilder.CreateIndex("IX_Ratings_UserId_GameId", "Ratings", new[] { "UserId", "GameId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Ratings_GameId", "Ratings", "GameId");
        migrationBuilder.CreateIndex("IX_Reviews_UserId_GameId", "Reviews", new[] { "UserId", "GameId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Reviews_GameId_CreatedAt", "Reviews", new[] { "GameId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Reviews_UserId_CreatedAt", "Reviews", new[] { "UserId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ReviewLikes_UserId", "ReviewLikes", "UserId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("ReviewLikes");
        migrationBuilder.DropTable("Reviews");
        migrationBuilder.DropTable("Ratings");
        migrationBuilder.DropTable("Playthroughs");
        migrationBuilder.DropTable("GamingDiaryEntries");
    }
}

