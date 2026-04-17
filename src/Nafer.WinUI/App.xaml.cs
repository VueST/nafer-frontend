using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Nafer.WinUI.Infrastructure.DependencyInjection;

namespace Nafer.WinUI;

public partial class App : Application
{
    private static IHost? _host;
    public static IHost HostInstance => _host ??= CreateHost();

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .UseContentRoot(AppContext.BaseDirectory)
            .ConfigureServices((_, services) =>
            {
                services.AddCoreServices();
                services.AddUIServices();
            })
            .Build();
    }

    public static T GetService<T>() where T : class =>
        HostInstance.Services.GetRequiredService<T>();

    public App()
    {
        InitializeComponent();

        // Architectural Clean Fix: Set RequestedTheme right after InitializeComponent
        // This stops the XAML framework from defaulting to 'Light' mode on cold boot.
        try
        {
            var themeService = GetService<IThemeSelectorService>();
            themeService.Initialize();
            RequestedTheme = themeService.Theme == Nafer.Core.Domain.Models.AppTheme.Dark 
                ? ApplicationTheme.Dark 
                : ApplicationTheme.Light;
        }
        catch { /* Settings fallback */ }

        UnhandledException += (s, e) =>
        {
            try { File.AppendAllText("unhandled_error.txt", e.Exception.ToString()); } catch { }
            System.Diagnostics.Debug.WriteLine($"[FATAL] {e.Exception}");
        };
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // Simple, clean instantiation. Window handles its own activation when Loaded.
            _ = new MainWindow { Title = "Nafer" };
        }
        catch (Exception ex)
        {
            File.WriteAllText("onlaunched_crash.txt", ex.ToString());
            throw;
        }
    }
}
