namespace Nafer.Core.Application.ViewModels.Auth;

/// <summary>
/// Orchestrates the Account flyout panel.
/// Reacts to session changes and exposes state for the three panel states:
///   Anonymous → Sign In / Sign Up tabs
///   Authenticated → User Profile panel
/// </summary>
public class AccountViewModel : ReactiveObject
{
    private readonly IAuthSessionService _sessionService;
    private readonly IAuthService _authService;

    [Reactive] public bool IsAuthenticated { get; private set; }
    [Reactive] public string UserEmail { get; private set; } = "Welcome To Nafer";
    [Reactive] public string UserName { get; private set; } = "Guest";
    [Reactive] public string UserInitials { get; private set; } = "G";

    // Role-based feature flags — bound directly to XAML Visibility
    [Reactive] public UserRole UserRole { get; private set; } = UserRole.User;
    [Reactive] public bool HasPremiumAccess { get; private set; }
    [Reactive] public bool CanModerate { get; private set; }
    [Reactive] public bool IsAdmin { get; private set; }

    /// <summary>Role badge label ("PREMIUM", "MOD", "ADMIN") or null for base User.</summary>
    [Reactive] public string? RoleBadgeLabel { get; private set; }

    /// <summary>True when a role badge should be visible.</summary>
    [Reactive] public bool IsRoleBadgeVisible { get; private set; }

    /// <summary>True while the logout network call is in-flight.</summary>
    [Reactive] public bool IsLoggingOut { get; private set; }

    public LoginViewModel Login { get; }
    public RegisterViewModel Register { get; }

    public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

    public AccountViewModel(
        IAuthSessionService sessionService,
        IAuthService authService,
        LoginViewModel loginViewModel,
        RegisterViewModel registerViewModel)
    {
        _sessionService = sessionService;
        _authService    = authService;
        Login           = loginViewModel;
        Register        = registerViewModel;

        // Subscribe to session changes — UI reacts automatically.
        _sessionService.SessionChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnSessionChanged);

        // Initialize immediately for the current session (handles app restart).
        OnSessionChanged(_sessionService.CurrentSession);

        // LogoutCommand: calls backend (best-effort) then clears local session.
        LogoutCommand = ReactiveCommand.CreateFromTask(
            ExecuteLogoutAsync,
            this.WhenAnyValue(x => x.IsAuthenticated));
    }

    // ── Private methods ───────────────────────────────────────────────────────

    private async Task ExecuteLogoutAsync()
    {
        var session = _sessionService.CurrentSession;
        if (session is null) return;

        IsLoggingOut = true;
        try
        {
            // Tell the backend to invalidate both tokens.
            // We don't await failures — local session clears regardless.
            await _authService.LogoutAsync(session.Token, session.RefreshToken);
        }
        finally
        {
            IsLoggingOut = false;
            // Always clear locally — even if the network call failed.
            _sessionService.ClearSession();
        }
    }

    private void OnSessionChanged(AuthToken? token)
    {
        IsAuthenticated = token is not null;

        if (token is null)
        {
            UserEmail        = "Welcome To Nafer";
            UserName         = "Guest";
            UserInitials     = "G";
            UserRole         = UserRole.User;
            HasPremiumAccess = false;
            CanModerate      = false;
            IsAdmin          = false;
            RoleBadgeLabel   = null;
            IsRoleBadgeVisible = false;
            return;
        }

        // Derive all display data from the token — no duplicate logic.
        var profile      = UserProfile.FromToken(token);
        UserEmail        = profile.Email;
        UserName         = profile.DisplayName;
        UserInitials     = profile.Initials;
        UserRole         = profile.Role;
        HasPremiumAccess = profile.HasPremiumAccess;
        CanModerate      = profile.CanModerate;
        IsAdmin          = profile.IsAdmin;
        RoleBadgeLabel   = profile.RoleBadgeLabel;
        IsRoleBadgeVisible = profile.RoleBadgeLabel is not null;
    }
}
