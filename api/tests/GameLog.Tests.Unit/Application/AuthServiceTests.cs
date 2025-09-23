using GameLog.Application.Auth;
using GameLog.Application.Common;
using GameLog.Domain.Entities;
using GameLog.Tests.Unit.Support;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GameLog.Tests.Unit.Application;

public class AuthServiceTests : IDisposable
{
    private const string Password = "correct horse battery";
    private readonly TestDb testDb = new();
    private readonly FakeClock clock = new(new DateTimeOffset(2025, 10, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly PasswordHasher<User> hasher = new();

    public void Dispose() => testDb.Dispose();

    private AuthService Service() => new(testDb.Create(), hasher, clock);

    private Task<User> Register(string username = "roger", string email = "roger@example.com", string password = Password) =>
        Service().RegisterAsync(new RegisterRequest(username, email, password, null));

    [Fact]
    public async Task Registration_stores_a_hash_never_the_password()
    {
        var user = await Register();

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.DoesNotContain(Password, user.PasswordHash);
        Assert.NotEqual(PasswordVerificationResult.Failed, hasher.VerifyHashedPassword(user, user.PasswordHash, Password));
        Assert.Equal("roger", user.DisplayName);
        Assert.Equal(GameLog.Domain.Enums.UserRole.User, user.Role);
    }

    [Fact]
    public async Task Usernames_are_unique_ignoring_case()
    {
        await Register("Roger", "a@example.com");
        var error = await Assert.ThrowsAsync<ConflictException>(() => Register("roger", "b@example.com"));
        Assert.Equal("username_taken", error.Code);
    }

    [Fact]
    public async Task Emails_are_unique_ignoring_case()
    {
        await Register("roger", "Roger@Example.com");
        var error = await Assert.ThrowsAsync<ConflictException>(() => Register("other", "roger@example.COM"));
        Assert.Equal("email_taken", error.Code);
    }

    [Fact]
    public async Task Invalid_registrations_report_every_problem_at_once()
    {
        var error = await Assert.ThrowsAsync<ValidationFailedException>(() => Register("x", "not-an-email", "short"));
        Assert.Contains("username", error.Errors.Keys);
        Assert.Contains("email", error.Errors.Keys);
        Assert.Contains("password", error.Errors.Keys);
    }

    [Fact]
    public async Task Login_works_with_username_or_email_in_any_case()
    {
        await Register("Roger", "roger@example.com");

        Assert.Equal("Roger", (await Service().LoginAsync(new LoginRequest("ROGER", Password))).Username);
        Assert.Equal("Roger", (await Service().LoginAsync(new LoginRequest("Roger@Example.com", Password))).Username);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_user_are_indistinguishable()
    {
        await Register();

        var wrong = await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest("roger", "nope-nope-nope")));
        var unknown = await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest("ghost", Password)));
        Assert.Equal(wrong.Message, unknown.Message);
    }

    [Fact]
    public async Task Five_failures_lock_the_account_even_for_the_right_password()
    {
        await Register();
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest("roger", "wrong-password")));
        }

        await Assert.ThrowsAsync<AccountLockedException>(() => Service().LoginAsync(new LoginRequest("roger", Password)));
    }

    [Fact]
    public async Task The_lockout_expires()
    {
        await Register();
        for (var i = 0; i < AuthService.MaxFailedAttempts; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest("roger", "wrong-password")));
        }

        clock.Advance(AuthService.LockoutDuration + TimeSpan.FromSeconds(1));
        Assert.Equal("roger", (await Service().LoginAsync(new LoginRequest("roger", Password))).Username);
    }

    [Fact]
    public async Task A_successful_login_resets_the_failure_counter()
    {
        await Register();
        for (var i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest("roger", "wrong-password")));
        }

        await Service().LoginAsync(new LoginRequest("roger", Password));
        // Four more failures must not lock, because the counter started again from zero.
        for (var i = 0; i < AuthService.MaxFailedAttempts - 1; i++)
        {
            await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest("roger", "wrong-password")));
        }

        Assert.Equal("roger", (await Service().LoginAsync(new LoginRequest("roger", Password))).Username);
    }

    [Fact]
    public async Task Blank_credentials_are_rejected_without_touching_the_database()
    {
        await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest("", "")));
        await Assert.ThrowsAsync<UnauthorizedException>(() => Service().LoginAsync(new LoginRequest(null!, null!)));
    }
}
