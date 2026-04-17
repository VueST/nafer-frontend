using Microsoft.UI.Xaml.Navigation;
using Nafer.WinUI.Infrastructure.Navigation;
using Nafer.Core.Application.Contracts;
using System.Reactive.Linq;

namespace Nafer.WinUI.Features.Shell;

public class ShellViewModel : ReactiveObject
{
    [Reactive] public object? Selected { get; set; }
    [Reactive] public bool IsBackEnabled { get; set; }
    [Reactive] public bool IsForwardEnabled { get; set; }

    public NavigationService NavigationService { get; }
    public NavigationViewService NavigationViewService { get; }
    public IAuthSessionService AuthSession { get; }
    public AccountViewModel Account { get; }
    public IUpdateService UpdateService { get; }
    public ILocalSettingsService Settings { get; }

    [Reactive] public double UpdateProgress { get; set; }
    [Reactive] public bool IsUpdateAvailable { get; set; }
    [Reactive] public bool IsDownloading { get; set; }
    [Reactive] public string UpdateVersion { get; set; } = "";
    
    private VersionInfo? _latestVersion;

    public ShellViewModel(
        NavigationService navigationService,
        NavigationViewService navigationViewService,
        IAuthSessionService authSession,
        AccountViewModel accountViewModel)
    {
        NavigationService = navigationService;
        NavigationViewService = navigationViewService;
        AuthSession = authSession;
        Account = accountViewModel;
        UpdateService = Nafer.WinUI.App.GetService<IUpdateService>();
        Settings = Nafer.WinUI.App.GetService<ILocalSettingsService>();

        UpdateService.DownloadProgressChanged += (s, e) => 
            UpdateProgress = e.ProgressPercentage;

        // Auto-check for updates
        _ = CheckForUpdatesAsync();

        // Reactive subscription to navigation events
        NavigationService.Navigated
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnNavigated);

        // Reactive subscription to auth state (optional: can be used for sidebar badges/profile)
        AuthSession.SessionChanged
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { /* Update auth-related properties if needed */ });

        // Sync initial state
        UpdateNavigationState();
    }

    public void GoBack() => NavigationService.GoBack();
    public void GoForward() => NavigationService.GoForward();

    private void UpdateNavigationState()
    {
        IsBackEnabled = NavigationService.CanGoBack;
        IsForwardEnabled = NavigationService.CanGoForward;
    }

    private void OnNavigated(NavigationEventArgs e)
    {
        UpdateNavigationState();

        if (e.Content is not null)
        {
            // Resolve the ViewModel type from the page's "ViewModel" property
            var vmType = e.Content.GetType().GetProperty("ViewModel")?.PropertyType;
            if (vmType is not null)
            {
                // Sync the sidebar selection
                var selectedItem = NavigationViewService.GetSelectedItem(vmType);
                if (selectedItem is not null)
                {
                    Selected = selectedItem;
                }
            }
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        _latestVersion = await UpdateService.CheckForUpdatesAsync();
        if (_latestVersion != null)
        {
            UpdateVersion = _latestVersion.Version.ToString();
            
            bool autoUpdate = Settings.Get("AutoUpdate", true);
            if (autoUpdate)
            {
                await StartUpdateAsync();
            }
            else
            {
                IsUpdateAvailable = true;
            }
        }
    }

    public async Task StartUpdateAsync()
    {
        if (_latestVersion == null) return;
        
        IsDownloading = true;
        IsUpdateAvailable = false;
        
        var tempFile = Path.Combine(Path.GetTempPath(), $"NaferSetup_{UpdateVersion}.msi");
        await UpdateService.DownloadUpdateAsync(_latestVersion, tempFile);
        
        UpdateService.InstallAndRestart(tempFile);
    }
}
