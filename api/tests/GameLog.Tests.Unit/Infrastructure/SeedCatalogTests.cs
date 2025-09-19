using GameLog.Application.Common;
using GameLog.Infrastructure.Seed;

namespace GameLog.Tests.Unit.Infrastructure;

public class SeedCatalogTests
{
    private static readonly IReadOnlyList<GameLog.Application.Catalog.ExternalGame> Games = SeedCatalog.Load();

    [Fact]
    public void The_catalog_is_large_enough_to_demo_search_and_discovery() => Assert.True(Games.Count >= 25);

    [Fact]
    public void External_ids_and_slugs_are_unique()
    {
        Assert.Equal(Games.Count, Games.Select(g => g.ExternalId).Distinct().Count());
        Assert.Equal(Games.Count, Games.Select(g => SlugGenerator.From(g.Title)).Distinct().Count());
    }

    [Fact]
    public void Every_game_is_complete_enough_to_display()
    {
        foreach (var g in Games)
        {
            Assert.False(string.IsNullOrWhiteSpace(g.Summary), g.Title);
            Assert.NotNull(g.ReleaseDate);
            Assert.NotEmpty(g.Genres);
            Assert.NotEmpty(g.Platforms);
            Assert.NotEmpty(g.GameModes);
            Assert.True(g.Title.Length <= 200 && g.Summary!.Length <= 4000);
        }
    }

    [Fact]
    public void Genre_and_platform_names_do_not_collide_after_slugging()
    {
        var genres = Games.SelectMany(g => g.Genres).Distinct().ToList();
        Assert.Equal(genres.Count, genres.Select(SlugGenerator.From).Distinct().Count());
        var platforms = Games.SelectMany(g => g.Platforms).Distinct().ToList();
        Assert.Equal(platforms.Count, platforms.Select(SlugGenerator.From).Distinct().Count());
    }
}
