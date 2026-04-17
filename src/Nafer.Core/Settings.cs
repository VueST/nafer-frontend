using System.Reflection;

namespace Nafer.Core;

public static class Settings
{
    public static Key<AppTheme> Theme { get; } = new("Theme", AppTheme.Default);
    public static Key<bool> AutoUpdate { get; } = new("AutoUpdate", true);
    public static Key<bool> MinimizeToTrayOnClose { get; } = new("MinimizeToTrayOnClose", false);
    public static Key<bool> StartMinimizedToTray { get; } = new("StartMinimizedToTray", false);
}
