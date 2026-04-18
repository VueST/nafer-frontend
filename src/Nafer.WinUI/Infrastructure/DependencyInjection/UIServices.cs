using Nafer.WinUI.Features.Auth;
using Nafer.Core.Application.ViewModels.Auth;
using Nafer.WinUI.Features.Home;
using Nafer.WinUI.Features.Settings;
using Nafer.WinUI.Features.Shell;
using Nafer.WinUI.Features.Admin;
using Nafer.WinUI.Features.Moderation;
using Nafer.WinUI.Infrastructure.Navigation;
using Nafer.Core.Application.Contracts;
using Nafer.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Nafer.WinUI.Infrastructure.DependencyInjection;

public static class UIServices
{
    public static IServiceCollection AddUIServices(this IServiceCollection services)
    {
        // Platform services
        services.AddSingleton<IThemeSelectorService, ThemeSelectorService>();
        services.AddSingleton<NavigationService>();
        services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());
        services.AddSingleton<NavigationViewService>();
        services.AddSingleton<IUpdateService, GitHubUpdateService>();

        // Shell
        services.AddSingleton<ShellViewModel>();
        services.AddTransient<ShellPage>();

        // Auth
        services.AddTransient<AccountViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<AccountFlyoutControl>();
        services.AddTransient<SignInControl>();
        services.AddTransient<SignUpControl>();
        services.AddTransient<UserProfileControl>();

        // Pages
        services.AddPageForNavigation<HomeViewModel, HomePage>();
        services.AddPageForNavigation<SettingsPageViewModel, SettingsPage>();
        services.AddPageForNavigation<AdminViewModel, AdminPage>();
        services.AddPageForNavigation<ModerationViewModel, ModerationPage>();

        return services;
    }

    private static IServiceCollection AddPageForNavigation<TViewModel, TView>(
        this IServiceCollection services)
        where TViewModel : class
        where TView : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TView>();
        services.AddTransient<PageTypeMapping>(
            _ => new PageTypeMapping(typeof(TViewModel), typeof(TView)));

        return services;
    }
}
