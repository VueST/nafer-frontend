using Microsoft.UI.Xaml;
using Nafer.Core.Application.Contracts;
using Nafer.Core.Domain.Models;

namespace Nafer.WinUI.Infrastructure.Navigation;

public class ThemeSelectorService : IThemeSelectorService
{
    private readonly ILocalSettingsService _localSettings;
    public AppTheme Theme { get; private set; }

    public ThemeSelectorService(ILocalSettingsService localSettings)
    {
        _localSettings = localSettings;
    }

    public void Initialize()
    {
        Theme = _localSettings.Get(Nafer.Core.Settings.Theme.Name, Nafer.Core.Settings.Theme.Default);
    }

    public void SetTheme(AppTheme theme)
    {
        Theme = theme;
        _localSettings.Save(Nafer.Core.Settings.Theme.Name, theme);
        SetRequestedTheme();
    }

    public void SetRequestedTheme()
    {
        if (MainWindow.Current?.Content is FrameworkElement root)
        {
            root.RequestedTheme = Theme switch
            {
                AppTheme.Light => ElementTheme.Light,
                AppTheme.Dark => ElementTheme.Dark,
                _ => ElementTheme.Default,
            };
        }
    }
}
