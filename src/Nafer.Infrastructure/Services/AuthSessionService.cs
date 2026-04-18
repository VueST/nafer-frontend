using System.Reactive.Linq;
using System.Reactive.Subjects;
using Nafer.Core.Application.Contracts;
using Nafer.Core.Domain.Models;

namespace Nafer.Infrastructure.Services;

/// <summary>
/// Reactive session store with automatic token refresh.
///
/// Responsibilities:
///   1. Restore session from disk on app start (auto-login).
///   2. Schedule a background refresh 2 minutes before the access token expires.
///   3. On refresh failure, clear the session (force logout).
///   4. Expose IObservable so any ViewModel reacts to session changes without polling.
/// </summary>
public sealed class AuthSessionService : IAuthSessionService, IDisposable
{
    // ── Persistence keys ──────────────────────────────────────────────────────
    private const string KeyToken        = "auth_access_token";
    private const string KeyRefreshToken = "auth_refresh_token";
    private const string KeyUserId       = "auth_user_id";
    private const string KeyEmail        = "auth_email";
    private const string KeyRole         = "auth_role";
    private const string KeyExpiresAt    = "auth_expires_at"; // ISO-8601 string

    // Refresh 2 minutes before expiry to avoid 401 windows.
    private static readonly TimeSpan RefreshLeadTime = TimeSpan.FromMinutes(2);

    private readonly BehaviorSubject<AuthToken?> _subject;
    private readonly ISecureSettingsService _settings;
    private readonly IAuthService _authService;
    private readonly ICredentialStorageService _secureStorage;
    private Timer? _refreshTimer;

    // ── IAuthSessionService ───────────────────────────────────────────────────

    public IObservable<AuthToken?> SessionChanged { get; }
    public AuthToken? CurrentSession => _subject.Value;
    public bool IsAuthenticated      => _subject.Value is not null;

    public UserProfile? CurrentUser =>
        CurrentSession is { } token ? UserProfile.FromToken(token) : null;

    // ── Constructor ───────────────────────────────────────────────────────────

    public AuthSessionService(
        ISecureSettingsService settings, 
        IAuthService authService,
        ICredentialStorageService secureStorage)
    {
        _settings      = settings;
        _authService   = authService;
        _secureStorage = secureStorage;

        // Restore whatever was persisted on the previous run.
        var restored = RestoreSession();

        _subject       = new BehaviorSubject<AuthToken?>(restored);
        SessionChanged = _subject.AsObservable();

        // Schedule refresh if we already have a session (handles app restarts).
        if (restored is not null)
            ScheduleRefresh(restored);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void SetSession(AuthToken token)
    {
        PersistSession(token);
        _subject.OnNext(token);
        ScheduleRefresh(token);
    }

    public void ClearSession()
    {
        _refreshTimer?.Dispose();
        _refreshTimer = null;
        EraseSession();
        _subject.OnNext(null);
    }

    /// <summary>
    /// Silently exchanges the refresh token for a new access + refresh pair.
    /// Returns true if a fresh token was obtained and the session was updated.
    /// Returns false if refresh was not needed, had no refresh token, or failed.
    /// Clears the session (force logout) on permanent failures.
    /// </summary>
    public async Task<bool> TryRefreshAsync()
    {
        var current = _subject.Value;
        if (current is null) return false;

        // Only refresh if the token is close to expiry or already expired.
        var timeLeft = current.ExpiresAt - DateTimeOffset.UtcNow;
        if (timeLeft > RefreshLeadTime) return false; // still plenty of time

        if (string.IsNullOrWhiteSpace(current.RefreshToken)) return false;

        try
        {
            var newToken = await _authService.RefreshAsync(current.RefreshToken);
            SetSession(newToken);
            return true;
        }
        catch
        {
            // Refresh token expired or was revoked — force logout.
            ClearSession();
            return false;
        }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _refreshTimer?.Dispose();
        _subject.OnCompleted();
        _subject.Dispose();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Schedules a one-shot timer to fire RefreshLeadTime before the token expires.
    /// On fire, it silently refreshes and reschedules itself for the next cycle.
    /// </summary>
    private void ScheduleRefresh(AuthToken token)
    {
        _refreshTimer?.Dispose();

        var timeLeft   = token.ExpiresAt - DateTimeOffset.UtcNow;
        var fireIn     = timeLeft - RefreshLeadTime;

        // If already within the lead window, fire almost immediately.
        if (fireIn < TimeSpan.Zero)
            fireIn = TimeSpan.FromSeconds(5);

        _refreshTimer = new Timer(
            callback: async _ => await TryRefreshAsync(),
            state:    null,
            dueTime:  fireIn,
            period:   Timeout.InfiniteTimeSpan); // one-shot — TryRefreshAsync reschedules via SetSession
    }

    // ── Persistence ───────────────────────────────────────────────────────────

    private AuthToken? RestoreSession()
    {
        var userId = _settings.Get<string>(KeyUserId, string.Empty);
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        // Retrieve sensitive tokens from secure vault
        var token        = _secureStorage.Get("Nafer_Auth", userId);
        var refreshToken = _secureStorage.Get("Nafer_Refresh", userId);
        
        var email        = _settings.Get<string>(KeyEmail, string.Empty);
        var roleStr      = _settings.Get<string>(KeyRole, nameof(UserRole.User));
        var expiresStr   = _settings.Get<string>(KeyExpiresAt, string.Empty);

        if (string.IsNullOrWhiteSpace(token))
            return null;

        var role = Enum.TryParse<UserRole>(roleStr, ignoreCase: true, out var parsed)
            ? parsed
            : UserRole.User;

        var expiresAt = DateTimeOffset.TryParse(expiresStr, out var dt)
            ? dt
            : DateTimeOffset.UtcNow; // treat as expired → will trigger refresh

        return new AuthToken(token, refreshToken ?? string.Empty, userId, email, role, expiresAt);
    }

    private void PersistSession(AuthToken token)
    {
        // 1. Secure storage for tokens
        _secureStorage.Save("Nafer_Auth", token.UserId, token.Token);
        if (!string.IsNullOrEmpty(token.RefreshToken))
            _secureStorage.Save("Nafer_Refresh", token.UserId, token.RefreshToken);

        // 2. Local settings for metadata
        _settings.Save(KeyUserId, token.UserId);
        _settings.Save(KeyEmail, token.Email);
        _settings.Save(KeyRole, token.Role.ToString());
        _settings.Save(KeyExpiresAt, token.ExpiresAt.ToString("O"));
    }

    private void EraseSession()
    {
        var userId = _subject.Value?.UserId ?? _settings.Get<string>(KeyUserId, string.Empty);
        
        if (!string.IsNullOrEmpty(userId))
        {
            _secureStorage.Remove("Nafer_Auth", userId);
            _secureStorage.Remove("Nafer_Refresh", userId);
        }

        _settings.Save(KeyUserId, string.Empty);
        _settings.Save(KeyEmail, string.Empty);
        _settings.Save(KeyRole, string.Empty);
        _settings.Save(KeyExpiresAt, string.Empty);
    }
}
