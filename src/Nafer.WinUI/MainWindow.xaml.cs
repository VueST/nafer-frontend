using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinUIEx;
using Nafer.WinUI.Extensions;

namespace Nafer.WinUI;

public sealed partial class MainWindow : WindowEx
{
    public static new MainWindow? Current { get; private set; }

    public MainWindow()
    {
        Current = this;
        
        // 1. Establish Native Identity immediately (Hidden Sync)
        this.UseImmersiveDarkModeEx(true, activate: false);
        this.SuppressNativeBorders();

        InitializeComponent();
        
        // Defer actual showing until the UI is ready
        AppTitleBar.Loaded += (s, e) => {
            this.UseImmersiveDarkModeEx(true, activate: true);
            this.Activate();
        };

        // 2. Modern Backdrop Initialization
        this.SystemBackdrop = new MicaBackdrop();

        // 3. Precise TitleBar Customization
        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ExtendsContentIntoTitleBar = true;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            
            AppTitleBar.SizeChanged += (s, e) => 
                this.SetDragRegionForCustomTitleBar(AppTitleBar, BrandingColumn, DragColumn);
        }

        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "WindowIcon.ico");
        if (File.Exists(iconPath))
            AppWindow.SetIcon(iconPath);

        UpdateTitleBarButtons(((FrameworkElement)Content).ActualTheme == ElementTheme.Dark);
        ((FrameworkElement)Content).ActualThemeChanged += (s, _) => 
            UpdateTitleBarButtons(s.ActualTheme == ElementTheme.Dark);

        var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
        AppVersionText.Text = version != null 
            ? $"{version.Major}.{version.Minor}.{version.Build}" 
            : "Unknown";
    }

    private void UpdateTitleBarButtons(bool isDark)
    {
        if (AppWindow.TitleBar is not { } tb) return;
        tb.ButtonForegroundColor = isDark ? Colors.White : Colors.Black;
        tb.ButtonHoverForegroundColor = isDark ? Colors.White : Colors.Black;
        tb.ButtonHoverBackgroundColor = isDark 
            ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF) 
            : Color.FromArgb(0x33, 0x00, 0x00, 0x00);
    }

    // Required for XAML binding from MainWindow.xaml
    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e) { }
    private void BackButton_Click(object sender, RoutedEventArgs e) => Shell.ViewModel.GoBack();
    private void ForwardButton_Click(object sender, RoutedEventArgs e) => Shell.ViewModel.GoForward();
    private void PaneButton_Click(object sender, RoutedEventArgs e) => Shell.TogglePane();
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter) presenter.Minimize();
    }

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            if (presenter.State == OverlappedPresenterState.Maximized)
            {
                presenter.Restore();
                MaximizeIcon.Glyph = "\uE922";
            }
            else
            {
                presenter.Maximize();
                MaximizeIcon.Glyph = "\uE923";
            }
        }
    }
}
