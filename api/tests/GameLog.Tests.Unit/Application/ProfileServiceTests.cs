using GameLog.Application.Common;
using GameLog.Application.Users;
using GameLog.Domain.Entities;
using GameLog.Tests.Unit.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameLog.Tests.Unit.Application;

public class ProfileServiceTests : IDisposable
{
    private readonly TestDb testDb = new();
    private readonly MemoryStorage storage = new();
    private readonly PasswordHasher<User> hasher = new();

    public void Dispose() => testDb.Dispose();

    private ProfileService Service(bool validImage = true) =>
        new(testDb.Create(), storage, new FakeImageProcessor(validImage), hasher, NullLogger<ProfileService>.Instance);

    private User NewUser(string name = "roger", string password = "correct horse battery")
    {
        using var db = testDb.Create();
        var user = Seed.User(db, name);
        user.PasswordHash = hasher.HashPassword(user, password);
        db.SaveChanges();
        return user;
    }

    [Fact]
    public async Task Public_profiles_expose_no_private_fields()
    {
        var user = NewUser();
        var profile = await Service().GetAsync("ROGER");

        Assert.Equal(user.Id, profile.Id);
        Assert.Equal("roger", profile.Username);
        Assert.DoesNotContain("Email", typeof(ProfileDto).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public async Task Unknown_profiles_are_404() => await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync("ghost"));

    [Fact]
    public async Task Updates_change_only_the_supplied_fields()
    {
        var user = NewUser();
        await Service().UpdateAsync(user.Id, new UpdateProfileRequest("Roger A.", "I like RPGs", true));
        var changed = await Service().UpdateAsync(user.Id, new UpdateProfileRequest(null, null, null));

        Assert.Equal("Roger A.", changed.DisplayName);
        Assert.True(changed.ShowSpoilersByDefault);
        Assert.Equal("I like RPGs", (await Service().GetAsync("roger")).Bio);
    }

    [Fact]
    public async Task An_empty_bio_clears_it()
    {
        var user = NewUser();
        await Service().UpdateAsync(user.Id, new UpdateProfileRequest(null, "hello", null));
        await Service().UpdateAsync(user.Id, new UpdateProfileRequest(null, "   ", null));
        Assert.Null((await Service().GetAsync("roger")).Bio);
    }

    [Fact]
    public async Task Invalid_values_are_rejected_and_nothing_is_saved()
    {
        var user = NewUser();
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Service().UpdateAsync(user.Id, new UpdateProfileRequest("", new string('x', 501), true)));

        Assert.Contains("displayName", error.Errors.Keys);
        Assert.Contains("bio", error.Errors.Keys);
        await using var db = testDb.Create();
        Assert.False((await db.Set<User>().AsNoTracking().SingleAsync()).ShowSpoilersByDefault);
    }

    [Fact]
    public async Task A_new_avatar_replaces_and_removes_the_old_file()
    {
        var user = NewUser();
        var first = await Service().SetAvatarAsync(user.Id, new MemoryStream([1]));
        var firstKey = first.AvatarUrl![MediaUrls.Prefix.Length..];
        var second = await Service().SetAvatarAsync(user.Id, new MemoryStream([1]));

        Assert.NotEqual(first.AvatarUrl, second.AvatarUrl);
        Assert.False(storage.Objects.ContainsKey(firstKey));
        Assert.Equal(1, storage.Objects.Count);
    }

    [Fact]
    public async Task An_invalid_image_changes_nothing()
    {
        var user = NewUser();
        await Assert.ThrowsAsync<ValidationFailedException>(() => Service(validImage: false).SetAvatarAsync(user.Id, new MemoryStream([1])));
        Assert.Empty(storage.Objects);
        Assert.Null((await Service().GetAsync("roger")).AvatarUrl);
    }

    [Fact]
    public async Task Removing_the_avatar_deletes_the_file()
    {
        var user = NewUser();
        await Service().SetAvatarAsync(user.Id, new MemoryStream([1]));
        await Service().RemoveAvatarAsync(user.Id);
        Assert.Empty(storage.Objects);
    }

    [Fact]
    public async Task Deleting_an_account_requires_the_password()
    {
        var user = NewUser();
        await Assert.ThrowsAsync<ValidationFailedException>(() => Service().DeleteAccountAsync(user.Id, "wrong password!"));
        Assert.Equal("roger", (await Service().GetAsync("roger")).Username);
    }

    [Fact]
    public async Task Deleting_an_account_removes_the_user_and_the_avatar()
    {
        var user = NewUser();
        await Service().SetAvatarAsync(user.Id, new MemoryStream([1]));
        await Service().DeleteAccountAsync(user.Id, "correct horse battery");

        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync("roger"));
        Assert.Empty(storage.Objects);
    }
}
