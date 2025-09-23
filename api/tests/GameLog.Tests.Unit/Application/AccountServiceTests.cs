using GameLog.Application.Auth;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Tests.Unit.Support;
using Microsoft.AspNetCore.Identity;

namespace GameLog.Tests.Unit.Application;

public class AccountServiceTests : IDisposable
{
    private const string Password = "correct horse battery";
    private readonly TestDb testDb = new();
    private readonly PasswordHasher<User> hasher = new();

    public void Dispose() => testDb.Dispose();

    private async Task<User> Registered()
    {
        var auth = new AuthService(testDb.Create(), hasher, new FakeClock(DateTimeOffset.UtcNow));
        return await auth.RegisterAsync(new RegisterRequest("roger", "roger@example.com", Password, null));
    }

    private AccountService Service() => new(testDb.Create(), hasher);

    [Fact]
    public async Task Changing_the_password_rotates_the_security_stamp_which_ends_other_sessions()
    {
        var user = await Registered();
        var before = user.SecurityStamp;

        var updated = await Service().ChangePasswordAsync(user.Id, new ChangePasswordRequest(Password, "a brand new passphrase"));

        Assert.NotEqual(before, updated.SecurityStamp);
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(updated, updated.PasswordHash, "a brand new passphrase"));
        Assert.Equal(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(updated, updated.PasswordHash, Password));
    }

    [Fact]
    public async Task The_current_password_must_be_correct()
    {
        var user = await Registered();
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Service().ChangePasswordAsync(user.Id, new ChangePasswordRequest("not my password", "a brand new passphrase")));
        Assert.Contains("currentPassword", error.Errors.Keys);
    }

    [Fact]
    public async Task The_new_password_must_satisfy_the_policy()
    {
        var user = await Registered();
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            Service().ChangePasswordAsync(user.Id, new ChangePasswordRequest(Password, "short")));
        Assert.Contains("newPassword", error.Errors.Keys);
    }

    [Fact]
    public async Task Unknown_users_are_404() =>
        await Assert.ThrowsAsync<NotFoundException>(() => Service().GetAsync(Guid.NewGuid()));
}
