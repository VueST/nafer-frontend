namespace Nafer.WinUI.Features.Settings;

public class SettingsPageViewModel : ReactiveObject
{
    private readonly ILocalSettingsService _settingsService;
    private readonly IThemeSelectorService _themeSelectorService;

    [Reactive] public AppTheme Theme { get; set; }
    [Reactive] public bool AutoUpdate { get; set; }
    [Reactive] public bool MinimizeToTrayOnClose { get; set; }
    [Reactive] public bool StartMinimizedToTray { get; set; }

    public IEnumerable<AppTheme> Themes { get; } = Enum.GetValues<AppTheme>();

    public string AppVersion { get; } =
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "1.0.0";

    public SettingsPageViewModel(ILocalSettingsService settingsService, IThemeSelectorService themeSelectorService)
    {
        _settingsService = settingsService;
        _themeSelectorService = themeSelectorService;

        Theme               = _settingsService.Get(Nafer.Core.Settings.Theme.Name,               Nafer.Core.Settings.Theme.Default);
        AutoUpdate          = _settingsService.Get(Nafer.Core.Settings.AutoUpdate.Name,          Nafer.Core.Settings.AutoUpdate.Default);
        MinimizeToTrayOnClose = _settingsService.Get(Nafer.Core.Settings.MinimizeToTrayOnClose.Name, Nafer.Core.Settings.MinimizeToTrayOnClose.Default);
        StartMinimizedToTray  = _settingsService.Get(Nafer.Core.Settings.StartMinimizedToTray.Name,  Nafer.Core.Settings.StartMinimizedToTray.Default);

        this.WhenAnyValue(x => x.Theme)
            .DistinctUntilChanged()
            .Skip(1)
            .Subscribe(t =>
            {
                _settingsService.Save(Nafer.Core.Settings.Theme.Name, t);
                _themeSelectorService.SetTheme(t);
            });

        this.WhenAnyValue(x => x.AutoUpdate).DistinctUntilChanged().Skip(1)
            .Subscribe(v => _settingsService.Save(Nafer.Core.Settings.AutoUpdate.Name, v));

        this.WhenAnyValue(x => x.MinimizeToTrayOnClose).DistinctUntilChanged().Skip(1)
            .Subscribe(v => _settingsService.Save(Nafer.Core.Settings.MinimizeToTrayOnClose.Name, v));

        this.WhenAnyValue(x => x.StartMinimizedToTray).DistinctUntilChanged().Skip(1)
            .Subscribe(v => _settingsService.Save(Nafer.Core.Settings.StartMinimizedToTray.Name, v));
    }
}
