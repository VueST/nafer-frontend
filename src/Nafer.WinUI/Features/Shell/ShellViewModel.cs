using Microsoft.UI.Xaml.Navigation;
using Nafer.WinUI.Infrastructure.Navigation;
using Nafer.Core.Application.Contracts;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using ReactiveUI;
using Splat;
using Nafer.Core.Application.Common;

namespace Nafer.WinUI.Features.Shell;

public class ShellViewModel : ViewModelBase
{
    [Reactive] public object? Selected { get; set; }
    [Reactive] public bool IsBackEnabled { get; set; }
    [Reactive] public bool IsForwardEnabled { get; set; }

    public NavigationService NavigationService { get; }
    public NavigationViewService NavigationViewService { get; }
    public IAuthSessionService AuthSession { get; }
    public AccountViewModel Account { get; }
    public IUpdateService UpdateService { get; }
    public ISecureSettingsService Settings { get; }

    [Reactive] public double UpdateProgress { get; set; }
    [Reactive] public bool IsUpdateAvailable { get; set; }
    [Reactive] public bool IsDownloading { get; set; }
    [Reactive] public string UpdateVersion { get; set; } = "";
    [Reactive] public bool ShowUpdateNotification { get; set; }
    [Reactive] public bool ShowDownloadProgress { get; set; }
    [Reactive] public bool IsDownloadIndicatorVisible { get; set; }
    
    private VersionInfo? _latestVersion;

    public ReactiveCommand<Unit, Unit> CheckUpdatesCommand { get; }
    public ReactiveCommand<Unit, Unit> StartUpdateCommand { get; }

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
        Settings = Nafer.WinUI.App.GetService<ISecureSettingsService>();

        CheckUpdatesCommand = ReactiveCommand.CreateFromTask(CheckForUpdatesAsync);
        StartUpdateCommand = ReactiveCommand.CreateFromTask(StartUpdateAsync);

        // Best Practice: Use Reactive observables for event patterns to ensure clean disposal
        Observable.FromEventPattern<DownloadProgressArgs>(
            h => UpdateService.DownloadProgressChanged += h,
            h => UpdateService.DownloadProgressChanged -= h)
            .Subscribe(e => UpdateProgress = e.EventArgs.ProgressPercentage)
            .DisposeWith(Disposables);

        // Load preferences
        ShowUpdateNotification = Settings.Get("ShowUpdateNotification", true);
        ShowDownloadProgress = Settings.Get("ShowDownloadProgress", true);

        // Sync download indicator visibility
        this.WhenAnyValue(x => x.IsDownloading, x => x.ShowDownloadProgress)
            .Select(tuple => tuple.Item1 && tuple.Item2)
            .Subscribe(visible => IsDownloadIndicatorVisible = visible)
            .DisposeWith(Disposables);

        // Auto-check for updates
        CheckUpdatesCommand.Execute()
            .Subscribe()
            .DisposeWith(Disposables);

        // Reactive subscription to navigation events
        NavigationService.Navigated
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnNavigated)
            .DisposeWith(Disposables);

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
            var vmType = e.Content.GetType().GetProperty("ViewModel")?.PropertyType;
            if (vmType is not null)
            {
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
        try
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
                    IsUpdateAvailable = ShowUpdateNotification;
                }
            }
        }
        catch (HttpRequestException ex)
        {
            LogHost.Default.Warn($"Network error while checking for updates: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            LogHost.Default.Warn("Update check timed out.");
        }
        catch (Exception ex)
        {
            LogHost.Default.Error(ex, "Unexpected error during update check");
        }
    }

    public async Task StartUpdateAsync()
    {
        if (_latestVersion == null) return;
        
        try
        {
            IsDownloading = true;
            IsUpdateAvailable = false;
            
            var tempFile = Path.Combine(Path.GetTempPath(), $"NaferSetup_{UpdateVersion}.msi");
            await UpdateService.DownloadUpdateAsync(_latestVersion, tempFile);
            
            UpdateService.InstallAndRestart(tempFile);
        }
        catch (Exception ex)
        {
            LogHost.Default.Error(ex, "Update download/install failed");
            IsDownloading = false;
        }
    }
}
