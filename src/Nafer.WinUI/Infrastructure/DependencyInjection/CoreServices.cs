using Nafer.Infrastructure.Persistence;
using Nafer.Infrastructure.Services;

namespace Nafer.WinUI.Infrastructure.DependencyInjection;

public static class CoreServices
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // 1. Persistence — no dependencies
        services.AddSingleton<ISecureSettingsService, SecureSettingsService>();

        // 2. HTTP Clients — no service dependencies
        services.AddHttpClient("identity", c =>
        {
            c.BaseAddress = new Uri(ApiEndpoints.Identity);
        });

        // 3. Auth: IAuthService first (AuthSessionService depends on it)
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IAuthSessionService, AuthSessionService>();

        return services;
    }
}
