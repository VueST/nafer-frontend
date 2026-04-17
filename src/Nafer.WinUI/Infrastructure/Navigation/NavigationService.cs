using System.Reactive.Subjects;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Splat;

namespace Nafer.WinUI.Infrastructure.Navigation;

/// <summary>
/// WinUI navigation service — type-safe, DI-driven.
/// </summary>
public class NavigationService : INavigationService, IEnableLogger
{
    private readonly IServiceProvider _services;
    private readonly IEnumerable<PageTypeMapping> _mappings;

    private Frame? _frame;
    private readonly Subject<NavigationEventArgs> _navigated = new();

    public IObservable<NavigationEventArgs> Navigated => _navigated;

    public NavigationService(
        IServiceProvider services,
        IEnumerable<PageTypeMapping> mappings)
    {
        _services = services;
        _mappings = mappings;
    }

    public Frame Frame
    {
        get => _frame!;
        set
        {
            if (_frame is not null) _frame.Navigated -= OnFrameNavigated;
            _frame = value;
            if (_frame is not null) _frame.Navigated += OnFrameNavigated;
        }
    }

    public bool CanGoBack => _frame?.CanGoBack ?? false;
    public bool CanGoForward => _frame?.CanGoForward ?? false;

    public bool GoBack()
    {
        if (!CanGoBack) return false;
        _frame!.GoBack();
        return true;
    }

    public bool GoForward()
    {
        if (!CanGoForward) return false;
        _frame!.GoForward();
        return true;
    }

    public bool NavigateTo<TViewModel>(object? parameter = null, bool clearNavigation = false)
        where TViewModel : class
        => NavigateTo(typeof(TViewModel), parameter, clearNavigation);

    public bool NavigateTo(Type viewModelType, object? parameter = null, bool clearNavigation = false)
    {
        var mapping = _mappings.FirstOrDefault(m => m.ViewModelType == viewModelType);
        if (mapping is null)
        {
            this.Log().Error($"No page registered for ViewModel: {viewModelType.Name}");
            return false;
        }

        // Avoid navigating to the same page if parameters are identical (standard browser behavior)
        if (_frame?.Content?.GetType() == mapping.PageType && parameter is null)
            return false;

        _frame!.Tag = clearNavigation;
        var navigated = _frame.Navigate(mapping.PageType, parameter);
        
        return navigated;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        if (sender is not Frame frame || frame.Content is null) return;

        // Clear backstack if flag was set
        if (frame.Tag is true)
        {
            frame.BackStack.Clear();
            frame.Tag = null;
        }

        // Identify the ViewModel type from our mappings
        var mapping = _mappings.FirstOrDefault(m => m.PageType == frame.Content.GetType());
        if (mapping is not null)
        {
            var viewModel = _services.GetService(mapping.ViewModelType);
            if (viewModel is not null)
            {
                // Inject ViewModel into the page
                var prop = frame.Content.GetType().GetProperty("ViewModel");
                prop?.SetValue(frame.Content, viewModel);
            }
        }

        // Notify subscribers (like ShellViewModel) that navigation occurred
        _navigated.OnNext(e);
    }
}
