using GameLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLog.Infrastructure.Persistence.Migrations;

/// <summary>Users and the game catalog.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20250813210000_InitialCreate")]
public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Username = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                UsernameNormalized = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                Email = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                EmailNormalized = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                DisplayName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                AvatarKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                ShowSpoilersByDefault = table.Column<bool>(type: "boolean", nullable: false),
                Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                SecurityStamp = table.Column<Guid>(type: "uuid", nullable: false),
                FailedLoginCount = table.Column<int>(type: "integer", nullable: false),
                LockoutEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_Users", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Genres",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_Genres", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Platforms",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                Slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_Platforms", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "Games",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Synopsis = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                ReleaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                Developer = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                Publisher = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                CoverKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                BackgroundKey = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                OfficialUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                GameModes = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                ExternalProvider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                ExternalId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                RatingCount = table.Column<int>(type: "integer", nullable: false),
                RatingSum = table.Column<int>(type: "integer", nullable: false),
                CompletedCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table => { table.PrimaryKey("PK_Games", x => x.Id); });

        migrationBuilder.CreateTable(
            name: "GameGenres",
            columns: table => new
            {
                GameId = table.Column<Guid>(type: "uuid", nullable: false),
                GenreId = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GameGenres", x => new { x.GameId, x.GenreId });
                table.ForeignKey(name: "FK_GameGenres_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_GameGenres_Genres_GenreId", column: x => x.GenreId, principalTable: "Genres", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "GamePlatforms",
            columns: table => new
            {
                GameId = table.Column<Guid>(type: "uuid", nullable: false),
                PlatformId = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GamePlatforms", x => new { x.GameId, x.PlatformId });
                table.ForeignKey(name: "FK_GamePlatforms_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_GamePlatforms_Platforms_PlatformId", column: x => x.PlatformId, principalTable: "Platforms", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Users_UsernameNormalized", "Users", "UsernameNormalized", unique: true);
        migrationBuilder.CreateIndex("IX_Users_EmailNormalized", "Users", "EmailNormalized", unique: true);
        migrationBuilder.CreateIndex("IX_Genres_Slug", "Genres", "Slug", unique: true);
        migrationBuilder.CreateIndex("IX_Genres_Name", "Genres", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Platforms_Slug", "Platforms", "Slug", unique: true);
        migrationBuilder.CreateIndex("IX_Platforms_Name", "Platforms", "Name", unique: true);
        migrationBuilder.CreateIndex("IX_Games_Slug", "Games", "Slug", unique: true);
        migrationBuilder.CreateIndex("IX_Games_ExternalProvider_ExternalId", "Games", new[] { "ExternalProvider", "ExternalId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Games_ReleaseDate", "Games", "ReleaseDate");
        migrationBuilder.CreateIndex("IX_Games_CompletedCount", "Games", "CompletedCount");
        migrationBuilder.CreateIndex("IX_Games_RatingCount", "Games", "RatingCount");
        migrationBuilder.CreateIndex("IX_GameGenres_GenreId", "GameGenres", "GenreId");
        migrationBuilder.CreateIndex("IX_GamePlatforms_PlatformId", "GamePlatforms", "PlatformId");

        // Title search: lower(Title) LIKE '%term%' is served by a trigram GIN index.
        migrationBuilder.Sql("CREATE INDEX \"IX_Games_Title_Trgm\" ON \"Games\" USING gin (lower(\"Title\") gin_trgm_ops);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("GamePlatforms");
        migrationBuilder.DropTable("GameGenres");
        migrationBuilder.DropTable("Games");
        migrationBuilder.DropTable("Platforms");
        migrationBuilder.DropTable("Genres");
        migrationBuilder.DropTable("Users");
    }
}
