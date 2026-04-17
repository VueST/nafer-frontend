namespace Nafer.Core.Application.ViewModels.Auth;

/// <summary>
/// Drives the Sign In form inside the sidebar flyout.
/// Fully platform-agnostic — no WinUI references.
/// </summary>
public class LoginViewModel : ReactiveObject
{
    private readonly IAuthService _authService;
    private readonly IAuthSessionService _sessionService;

    [Reactive] public string Email { get; set; } = string.Empty;
    [Reactive] public string Password { get; set; } = string.Empty;
    [Reactive] public string ErrorMessage { get; set; } = string.Empty;
    [Reactive] public bool IsLoading { get; set; }
    [Reactive] public bool HasError { get; set; }

    public ReactiveCommand<Unit, AuthToken?> LoginCommand { get; }

    public LoginViewModel(IAuthService authService, IAuthSessionService sessionService)
    {
        _authService = authService;
        _sessionService = sessionService;

        this.WhenAnyValue(x => x.ErrorMessage)
            .Select(x => !string.IsNullOrEmpty(x))
            .Subscribe(x => HasError = x);

        var canLogin = this.WhenAnyValue(
            x => x.Email,
            x => x.Password,
            x => x.IsLoading,
            (email, pwd, loading) =>
                !string.IsNullOrWhiteSpace(email) &&
                !string.IsNullOrWhiteSpace(pwd) &&
                !loading);

        LoginCommand = ReactiveCommand.CreateFromTask(ExecuteLoginAsync, canLogin);

        LoginCommand.ThrownExceptions
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ex => ErrorMessage = ex.Message);
    }

    private async Task<AuthToken?> ExecuteLoginAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var token = await _authService.LoginAsync(Email, Password);
            _sessionService.SetSession(token);
            Reset();
            return token;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Reset()
    {
        Email = string.Empty;
        Password = string.Empty;
    }
}
