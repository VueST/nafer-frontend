namespace Nafer.Core.Application.Contracts;

/// <summary>
/// Platform-agnostic navigation abstraction.
/// </summary>
public interface INavigationService
{
    bool CanGoBack { get; }
    bool CanGoForward { get; }
    bool GoBack();
    bool GoForward();
    bool NavigateTo(Type viewModelType, object? parameter = null, bool clearNavigation = false);
    bool NavigateTo<TViewModel>(object? parameter = null, bool clearNavigation = false)
        where TViewModel : class;
}
