using System.Reactive.Subjects;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Splat;
using Nafer.Core.Application.Common;

namespace Nafer.WinUI.Infrastructure.Navigation;

/// <summary>
/// WinUI navigation service — type-safe, DI-driven, and memory-safe.
/// Manages a manual ViewModel stack to handle active disposal and prevent leaks.
/// </summary>
public class NavigationService : INavigationService, IEnableLogger, IDisposable
{
    private readonly IServiceProvider _services;
    private readonly IEnumerable<PageTypeMapping> _mappings;
    private readonly Core.Application.Contracts.IAuthSessionService _authSession;

    private Frame? _frame;
    private readonly Subject<NavigationEventArgs> _navigated = new();
    
    // --- Memory Management ---
    private readonly List<ViewModelBase> _backStack = new();
    private ViewModelBase? _currentViewModel;
    private const int MaxBackStackSize = 10;

    public IObservable<NavigationEventArgs> Navigated => _navigated;

    public NavigationService(
        IServiceProvider services,
        IEnumerable<PageTypeMapping> mappings,
        Core.Application.Contracts.IAuthSessionService authSession)
    {
        _services = services;
        _mappings = mappings;
        _authSession = authSession;
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
        
        // Pop from our manual stack first
        if (_backStack.Count > 0)
        {
            var oldVm = _currentViewModel;
            _currentViewModel = _backStack[^1];
            _backStack.RemoveAt(_backStack.Count - 1);
            
            // Dispose the VM that was just left (if it's not being held anywhere else)
            // Note: In a frame skip, we might have more complex logic, but for standard back:
            oldVm?.Dispose();
        }

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
            LogHost.Default.Error($"No page registered for ViewModel: {viewModelType.Name}");
            return false;
        }

        // --- RBAC Implementation ---
        var authAttr = viewModelType.GetCustomAttributes(typeof(Core.Domain.Attributes.AuthorizeAttribute), true)
            .FirstOrDefault() as Core.Domain.Attributes.AuthorizeAttribute;
        
        if (authAttr != null)
        {
            var userRole = _authSession.CurrentSession?.Role ?? Core.Domain.Models.UserRole.User;
            if (userRole < authAttr.RequiredRole)
            {
                LogHost.Default.Warn($"Access denied to {viewModelType.Name}. Required: {authAttr.RequiredRole}, Current: {userRole}");
                return false;
            }
        }

        // Avoid navigating to the same page if parameters are identical (standard browser behavior)
        if (_frame?.Content?.GetType() == mapping.PageType && parameter is null)
            return false;

        // --- Memory Stack Management ---
        if (clearNavigation)
        {
            ClearBackStack();
        }
        else if (_currentViewModel != null)
        {
            _backStack.Add(_currentViewModel);
            if (_backStack.Count > MaxBackStackSize)
            {
                var oldest = _backStack[0];
                _backStack.RemoveAt(0);
                oldest.Dispose();
            }
        }

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
            // Resolve or update the current ViewModel
            if (_currentViewModel?.GetType() != mapping.ViewModelType)
            {
                _currentViewModel = _services.GetService(mapping.ViewModelType) as ViewModelBase;
            }

            if (_currentViewModel is not null)
            {
                // Inject ViewModel into the page
                var prop = frame.Content.GetType().GetProperty("ViewModel");
                prop?.SetValue(frame.Content, _currentViewModel);
            }
        }

        // Notify subscribers (like ShellViewModel) that navigation occurred
        _navigated.OnNext(e);
    }

    public void ClearBackStack()
    {
        foreach (var vm in _backStack) vm.Dispose();
        _backStack.Clear();
        _currentViewModel?.Dispose();
        _currentViewModel = null;
    }

    public void Dispose()
    {
        ClearBackStack();
        _navigated.Dispose();
    }
}
