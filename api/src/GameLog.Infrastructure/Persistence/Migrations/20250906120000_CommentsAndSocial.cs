using GameLog.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameLog.Infrastructure.Persistence.Migrations;

/// <summary>Comments, likes, follows, activity feed and notifications.</summary>
[DbContext(typeof(AppDbContext))]
[Migration("20250906120000_CommentsAndSocial")]
public class CommentsAndSocial : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Comments",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: true),
                ReviewId = table.Column<Guid>(type: "uuid", nullable: true),
                ParentCommentId = table.Column<Guid>(type: "uuid", nullable: true),
                Body = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                IsSpoiler = table.Column<bool>(type: "boolean", nullable: false),
                LikeCount = table.Column<int>(type: "integer", nullable: false),
                ReplyCount = table.Column<int>(type: "integer", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                EditedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Comments", x => x.Id);
                table.ForeignKey(name: "FK_Comments_Users_UserId", column: x => x.UserId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Comments_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Comments_Reviews_ReviewId", column: x => x.ReviewId, principalTable: "Reviews", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Comments_Comments_ParentCommentId", column: x => x.ParentCommentId, principalTable: "Comments", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_Comments_OneTarget", "(\"GameId\" IS NOT NULL AND \"ReviewId\" IS NULL) OR (\"GameId\" IS NULL AND \"ReviewId\" IS NOT NULL)");
            });

        migrationBuilder.CreateTable(
            name: "CommentLikes",
            columns: table => new
            {
                CommentId = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CommentLikes", x => new { x.CommentId, x.UserId });
                table.ForeignKey(name: "FK_CommentLikes_Comments_CommentId", column: x => x.CommentId, principalTable: "Comments", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_CommentLikes_Users_UserId", column: x => x.UserId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserFollows",
            columns: table => new
            {
                FollowerId = table.Column<Guid>(type: "uuid", nullable: false),
                FolloweeId = table.Column<Guid>(type: "uuid", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserFollows", x => new { x.FollowerId, x.FolloweeId });
                table.ForeignKey(name: "FK_UserFollows_Users_FollowerId", column: x => x.FollowerId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_UserFollows_Users_FolloweeId", column: x => x.FolloweeId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.CheckConstraint("CK_UserFollows_NotSelf", "\"FollowerId\" <> \"FolloweeId\"");
            });

        migrationBuilder.CreateTable(
            name: "ActivityFeed",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: true),
                ReviewId = table.Column<Guid>(type: "uuid", nullable: true),
                CollectionId = table.Column<Guid>(type: "uuid", nullable: true),
                Value = table.Column<int>(type: "integer", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ActivityFeed", x => x.Id);
                table.ForeignKey(name: "FK_ActivityFeed_Users_ActorId", column: x => x.ActorId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_ActivityFeed_Games_GameId", column: x => x.GameId, principalTable: "Games", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Notifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RecipientId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                Type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                GameId = table.Column<Guid>(type: "uuid", nullable: true),
                ReviewId = table.Column<Guid>(type: "uuid", nullable: true),
                CommentId = table.Column<Guid>(type: "uuid", nullable: true),
                ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Notifications", x => x.Id);
                table.ForeignKey(name: "FK_Notifications_Users_RecipientId", column: x => x.RecipientId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
                table.ForeignKey(name: "FK_Notifications_Users_ActorId", column: x => x.ActorId, principalTable: "Users", principalColumn: "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Comments_GameId_CreatedAt", "Comments", new[] { "GameId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Comments_GameId_LikeCount", "Comments", new[] { "GameId", "LikeCount" });
        migrationBuilder.CreateIndex("IX_Comments_ReviewId_CreatedAt", "Comments", new[] { "ReviewId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Comments_ParentCommentId_CreatedAt", "Comments", new[] { "ParentCommentId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_Comments_UserId", "Comments", "UserId");
        migrationBuilder.CreateIndex("IX_CommentLikes_UserId", "CommentLikes", "UserId");
        migrationBuilder.CreateIndex("IX_UserFollows_FolloweeId", "UserFollows", "FolloweeId");
        migrationBuilder.CreateIndex("IX_ActivityFeed_ActorId_CreatedAt", "ActivityFeed", new[] { "ActorId", "CreatedAt" });
        migrationBuilder.CreateIndex("IX_ActivityFeed_CreatedAt", "ActivityFeed", "CreatedAt");
        migrationBuilder.CreateIndex("IX_Notifications_RecipientId_ReadAt_CreatedAt", "Notifications", new[] { "RecipientId", "ReadAt", "CreatedAt" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Notifications");
        migrationBuilder.DropTable("ActivityFeed");
        migrationBuilder.DropTable("UserFollows");
        migrationBuilder.DropTable("CommentLikes");
        migrationBuilder.DropTable("Comments");
    }
}

