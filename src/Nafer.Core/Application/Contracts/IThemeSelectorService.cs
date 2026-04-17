namespace Nafer.Core.Application.Contracts;

public interface IThemeSelectorService
{
    AppTheme Theme { get; }
    void Initialize();
    void SetTheme(AppTheme theme);
    void SetRequestedTheme();
}
