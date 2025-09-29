using GameLog.Application.Common;
using GameLog.Application.Diary;
using GameLog.Domain.Entities;
using GameLog.Domain.Enums;
using GameLog.Infrastructure.Persistence;
using GameLog.Tests.Unit.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameLog.Tests.Unit.Application;

public class DiaryServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2025, 10, 1);
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly User roger;
    private readonly User ana;
    private readonly Game hades;

    public DiaryServiceTests()
    {
        using var db = testDb.Create();
        roger = Seed.User(db, "roger");
        ana = Seed.User(db, "ana");
        hades = Seed.Game(db, "Hades", platforms: ["PC", "Switch"]);
    }

    public void Dispose() => testDb.Dispose();

    private DiaryService Svc() => new(testDb.Create(), clock, NullLogger<DiaryService>.Instance);

    private static UpsertDiaryRequest Req(
        DiaryStatus status, DateOnly? start = null, DateOnly? done = null, int? minutes = null, int? platform = null,
        bool? recommended = null, string? notes = null) => new(status, platform, start, done, minutes, recommended, notes);

    private async Task<int> CompletedCount()
    {
        await using var db = testDb.Create();
        return (await db.Set<Game>().AsNoTracking().SingleAsync(g => g.Id == hades.Id)).CompletedCount;
    }

    [Fact]
    public async Task Starting_a_game_creates_the_entry_and_its_first_playthrough()
    {
        var entry = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Playing, new(2025, 9, 20), minutes: 90, notes: " great "));

        Assert.Equal(DiaryStatus.Playing, entry.Status);
        var run = Assert.Single(entry.Playthroughs);
        Assert.Equal(1, run.Number);
        Assert.Equal(new DateOnly(2025, 9, 20), run.StartedOn);
        Assert.Equal(90, entry.TotalPlaytimeMinutes);
        Assert.Equal("great", entry.Notes);
        Assert.Equal("Hades", entry.GameTitle);
    }

    [Fact]
    public async Task Saving_twice_updates_the_same_entry()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Playing, minutes: 10));
        var second = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.OnHold, minutes: 25));

        Assert.Equal(DiaryStatus.OnHold, second.Status);
        Assert.Single(second.Playthroughs);
        Assert.Equal(25, second.TotalPlaytimeMinutes);
        await using var db = testDb.Create();
        Assert.Equal(1, await db.Set<GamingDiaryEntry>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Completing_defaults_the_date_to_today_and_counts_the_completion()
    {
        var entry = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed, new(2025, 9, 1), minutes: 1800));

        Assert.Equal(Today, entry.CompletedOn);
        Assert.Equal(1, await CompletedCount());
    }

    [Fact]
    public async Task The_completed_counter_follows_completions_and_removals_across_users()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed));
        await Svc().UpsertAsync(ana.Id, hades.Id, Req(DiaryStatus.Completed));
        Assert.Equal(2, await CompletedCount());

        await Svc().UpsertAsync(ana.Id, hades.Id, Req(DiaryStatus.Abandoned));
        Assert.Equal(1, await CompletedCount());

        await Svc().DeleteAsync(roger.Id, hades.Id);
        Assert.Equal(0, await CompletedCount());
    }

    [Fact]
    public async Task Picking_up_a_completed_game_again_starts_a_new_playthrough()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed, new(2025, 9, 1), new(2025, 9, 20), 600));
        var replay = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Playing, new(2025, 9, 25), minutes: 120));

        Assert.Equal(2, replay.Playthroughs.Count);
        Assert.Equal(new[] { 1, 2 }, replay.Playthroughs.Select(p => p.Number).ToArray());
        Assert.Equal(new DateOnly(2025, 9, 20), replay.Playthroughs[0].CompletedOn);
        Assert.Null(replay.CompletedOn); // the latest run is not finished
        Assert.Equal(720, replay.TotalPlaytimeMinutes);
        Assert.Equal(0, await CompletedCount());
    }

    [Fact]
    public async Task Re_saving_a_completed_entry_keeps_its_completion_date()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed, done: new(2025, 9, 20)));
        var again = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed, notes: "edited"));

        Assert.Equal(new DateOnly(2025, 9, 20), again.CompletedOn);
        Assert.Single(again.Playthroughs);
    }

    [Fact]
    public async Task A_wishlist_entry_has_no_playthroughs_and_cannot_carry_progress()
    {
        var wish = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.WantToPlay));
        Assert.Empty(wish.Playthroughs);

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.WantToPlay, minutes: 30)));
    }

    [Fact]
    public async Task A_game_that_was_played_cannot_return_to_the_wishlist()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Playing));
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.WantToPlay)));
        Assert.Contains("status", error.Errors.Keys);
    }

    [Fact]
    public async Task Invalid_dates_playtime_and_platforms_are_rejected_together()
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed, new(2025, 10, 5), new(2025, 9, 1), -5, platform: 9999)));

        Assert.Contains("startedOn", error.Errors.Keys);
        Assert.Contains("completedOn", error.Errors.Keys);
        Assert.Contains("totalPlaytimeMinutes", error.Errors.Keys);
        Assert.Contains("platformId", error.Errors.Keys);
        await using var db = testDb.Create();
        Assert.Equal(0, await db.Set<GamingDiaryEntry>().AsNoTracking().CountAsync());
    }

    [Fact]
    public async Task Unknown_games_are_404() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().UpsertAsync(roger.Id, Guid.NewGuid(), Req(DiaryStatus.Playing)));

    [Fact]
    public async Task Entries_include_the_users_own_rating()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed));
        await using (var db = testDb.Create())
        {
            db.Set<Rating>().Add(new Rating { UserId = roger.Id, GameId = hades.Id, HalfStars = 9, CreatedAt = Seed.Now, UpdatedAt = Seed.Now });
            await db.SaveChangesAsync();
        }

        Assert.Equal(9, (await Svc().GetAsync(roger.Id, hades.Id)).RatingHalfStars);
    }

    [Fact]
    public async Task Users_only_ever_see_and_change_their_own_entries()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Playing));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().GetAsync(ana.Id, hades.Id));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().DeleteAsync(ana.Id, hades.Id));
    }

    [Fact]
    public async Task Another_users_playthrough_cannot_be_edited_or_deleted()
    {
        var entry = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Playing, minutes: 5));
        var id = entry.Playthroughs[0].Id;

        await Assert.ThrowsAsync<NotFoundException>(() => Svc().UpdatePlaythroughAsync(ana.Id, id, new PlaythroughRequest(null, null, 999, null, null)));
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().DeletePlaythroughAsync(ana.Id, id));
        Assert.Equal(5, (await Svc().GetAsync(roger.Id, hades.Id)).TotalPlaytimeMinutes);
    }

    [Fact]
    public async Task Playthroughs_can_be_added_edited_and_removed_with_totals_kept_in_sync()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed, new(2025, 8, 1), new(2025, 8, 20), 300));
        var added = await Svc().AddPlaythroughAsync(roger.Id, hades.Id, new PlaythroughRequest(new(2025, 9, 1), null, 100, null, "hard mode"));

        Assert.Equal(400, added.TotalPlaytimeMinutes);
        Assert.Equal(DiaryStatus.Playing, added.Status);

        var secondId = added.Playthroughs[1].Id;
        var finished = await Svc().UpdatePlaythroughAsync(roger.Id, secondId, new PlaythroughRequest(new(2025, 9, 1), new(2025, 9, 15), 250, null, null));
        Assert.Equal(DiaryStatus.Completed, finished.Status);
        Assert.Equal(550, finished.TotalPlaytimeMinutes);

        var removed = await Svc().DeletePlaythroughAsync(roger.Id, secondId);
        Assert.Equal(300, removed.TotalPlaytimeMinutes);
        Assert.Single(removed.Playthroughs);
    }

    [Fact]
    public async Task The_last_playthrough_cannot_be_deleted()
    {
        var entry = await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Playing));
        await Assert.ThrowsAsync<ValidationFailedException>(() => Svc().DeletePlaythroughAsync(roger.Id, entry.Playthroughs[0].Id));
    }

    [Fact]
    public async Task Playthroughs_cannot_be_added_to_a_wishlist_entry()
    {
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.WantToPlay));
        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Svc().AddPlaythroughAsync(roger.Id, hades.Id, new PlaythroughRequest(null, null, null, null, null)));
    }

    [Fact]
    public async Task Public_diaries_can_be_listed_and_filtered_by_status()
    {
        var other = Seed.Game(testDb.Create(), "Celeste");
        await Svc().UpsertAsync(roger.Id, hades.Id, Req(DiaryStatus.Completed));
        await Svc().UpsertAsync(roger.Id, other.Id, Req(DiaryStatus.WantToPlay));

        Assert.Equal(2, (await Svc().ListAsync("ROGER", null, null, null)).Total);
        var wishlist = await Svc().ListAsync("roger", DiaryStatus.WantToPlay, null, null);
        Assert.Equal("Celeste", Assert.Single(wishlist.Items).GameTitle);
        Assert.Equal(0, (await Svc().ListAsync("ana", null, null, null)).Total);
        await Assert.ThrowsAsync<NotFoundException>(() => Svc().ListAsync("ghost", null, null, null));
    }
}
