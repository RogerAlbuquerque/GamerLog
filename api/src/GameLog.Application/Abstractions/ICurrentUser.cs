using GameLog.Application.Common;

namespace GameLog.Application.Abstractions;

/// <summary>
/// The authenticated caller, as established by the API layer. Services take their notion of
/// "who is acting" from here and never from request bodies (ownership is never client-supplied).
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    bool IsAdmin { get; }

    bool IsAuthenticated => UserId is not null;

    /// <exception cref="UnauthorizedException">There is no authenticated user.</exception>
    Guid RequireUserId() => UserId ?? throw new UnauthorizedException();
}
