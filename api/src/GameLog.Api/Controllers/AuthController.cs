using GameLog.Api.Extensions;
using GameLog.Application.Abstractions;
using GameLog.Application.Auth;
using GameLog.Domain.Entities;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GameLog.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(
    AuthService auth,
    AccountService account,
    IAntiforgery antiforgery,
    ICurrentUser current) : ControllerBase
{
    public sealed record CsrfResponse(string Token);

    /// <param name="CsrfToken">A request token bound to the (new) identity; send it as X-CSRF-TOKEN.</param>
    public sealed record AuthResponse(SessionUser User, string CsrfToken);

    [HttpGet("csrf")]
    [AllowAnonymous]
    public CsrfResponse Csrf() => new(NewCsrfToken());

    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.Auth)]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var user = await auth.RegisterAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, await SignInAsync(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitingExtensions.Auth)]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await auth.LoginAsync(request, ct);
        return await SignInAsync(user);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<CsrfResponse> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        HttpContext.User = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity());
        return new CsrfResponse(NewCsrfToken());
    }

    [HttpGet("me")]
    public async Task<SessionUser> Me(CancellationToken ct) =>
        SessionUser.From(await account.GetAsync(current.RequireUserId(), ct));

    [HttpPost("password")]
    [EnableRateLimiting(RateLimitingExtensions.Auth)]
    public async Task<AuthResponse> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await account.ChangePasswordAsync(current.RequireUserId(), request, ct);
        return await SignInAsync(user);
    }

    private async Task<AuthResponse> SignInAsync(User user)
    {
        var principal = AuthExtensions.CreatePrincipal(user);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            new AuthenticationProperties { IsPersistent = true });
        HttpContext.User = principal; // so the antiforgery token is bound to the new identity
        return new AuthResponse(SessionUser.From(user), NewCsrfToken());
    }

    private string NewCsrfToken() => antiforgery.GetAndStoreTokens(HttpContext).RequestToken!;
}
