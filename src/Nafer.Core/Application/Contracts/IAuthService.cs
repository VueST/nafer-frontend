namespace Nafer.Core.Application.Contracts;

/// <summary>
/// Defines remote authentication operations against the identity-service.
/// Each method maps 1:1 to a backend endpoint.
/// </summary>
public interface IAuthService
{
    /// <summary>POST /api/v1/auth/login — validates credentials and returns both tokens.</summary>
    Task<AuthToken> LoginAsync(string email, string password);

    /// <summary>POST /api/v1/auth/register — creates a new account and returns both tokens.</summary>
    Task<AuthToken> RegisterAsync(string email, string password);

    /// <summary>
    /// POST /api/v1/auth/logout — invalidates the access token (Redis denylist)
    /// and revokes the refresh token. Both tokens are sent to the backend.
    /// Best-effort: UI clears the local session regardless of network outcome.
    /// </summary>
    Task LogoutAsync(string accessToken, string refreshToken);

    /// <summary>
    /// POST /api/v1/auth/refresh — exchanges a valid refresh token for a new token pair.
    /// The old refresh token is consumed (one-time use on the backend).
    /// </summary>
    Task<AuthToken> RefreshAsync(string refreshToken);
}
