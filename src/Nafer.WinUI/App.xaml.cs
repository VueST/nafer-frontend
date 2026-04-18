using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Nafer.WinUI.Infrastructure.DependencyInjection;
using Nafer.Core.Application.Contracts;
using Serilog;
using Splat;
using Splat.Serilog;

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
        InitializeLogging();
        InitializeComponent();

        // Architectural Clean Fix: Set RequestedTheme right after InitializeComponent
        try
        {
            var themeService = GetService<IThemeSelectorService>();
            themeService.Initialize();
            RequestedTheme = themeService.Theme == Nafer.Core.Domain.Models.AppTheme.Dark 
                ? ApplicationTheme.Dark 
                : ApplicationTheme.Light;
        }
        catch { /* Settings fallback during boot */ }

        // --- Global Resilience: Comprehensive Exception Management ---
        
        // 1. UI Thread (XAML) Exceptions
        UnhandledException += (s, e) =>
        {
            Log.Fatal(e.Exception, "Unhandled XAML Exception in UI Thread");
            e.Handled = true; // Attempt to keep app alive
        };

        // 2. Background Task Exceptions
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Fatal(e.Exception, "Unobserved Task Exception");
            e.SetObserved();
        };

        // 3. Generic AppDomain Exceptions
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            Log.Fatal(e.ExceptionObject as Exception, "Critical AppDomain Exception");
        };
    }

    private void InitializeLogging()
    {
        var logPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Nafer", "logs", "nafer_.log");
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 7)
            .CreateLogger();

        // Connect Splat (ReactiveUI logging) to Serilog
        Locator.CurrentMutable.UseSerilogFullLogger();
        
        Log.Information("--- Nafer Application Starting (v1.3.3) ---");
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
            Log.Fatal(ex, "Critical failure during Application Launch");
            throw;
        }
    }
}
