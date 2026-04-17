namespace Nafer.Core.Application.Contracts;

/// <summary>
/// Single source of truth for the current authentication session.
/// Reactive: all UI subscribes to SessionChanged instead of polling.
/// Handles token persistence, auto-restore on app restart, and silent token refresh.
/// </summary>
public interface IAuthSessionService
{
    /// <summary>Emits the new token on login/refresh, null on logout or expiry.</summary>
    IObservable<AuthToken?> SessionChanged { get; }

    /// <summary>Current session snapshot — null if not authenticated.</summary>
    AuthToken? CurrentSession { get; }

    /// <summary>
    /// A permission-aware profile derived from the current session.
    /// Null when the user is not authenticated.
    /// </summary>
    UserProfile? CurrentUser { get; }

    /// <summary>True when a valid (non-null) session exists.</summary>
    bool IsAuthenticated { get; }

    /// <summary>Stores the token, persists it, and notifies all subscribers.</summary>
    void SetSession(AuthToken token);

    /// <summary>Clears the token from memory and disk, and notifies all subscribers.</summary>
    void ClearSession();

    /// <summary>
    /// Attempts a silent token refresh. If the access token is expired (or will
    /// expire within the next 2 minutes) and a refresh token exists, this method
    /// calls the backend to issue a new token pair.
    /// Returns true if a refresh was performed successfully.
    /// Returns false if refresh was not needed, not possible, or failed.
    /// </summary>
    Task<bool> TryRefreshAsync();
}
