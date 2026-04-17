using Microsoft.UI.Xaml.Controls;

namespace Nafer.WinUI.Infrastructure.Navigation;

/// <summary>
/// Manages the NavigationView control — selected item sync.
/// </summary>
public class NavigationViewService
{
    private NavigationView? _navigationView;
    private readonly NavigationService _navigationService;

    public NavigationViewService(NavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.ItemInvoked += OnItemInvoked;
    }

    public void UnInitialize()
    {
        if (_navigationView is null) return;
        _navigationView.ItemInvoked -= OnItemInvoked;
    }

    public NavigationViewItem? GetSelectedItem(Type viewModelType)
    {
        return GetAllNavigationItems()
            .FirstOrDefault(item => GetNavigateToType(item) == viewModelType);
    }

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer is NavigationViewItem item)
        {
            var type = GetNavigateToType(item);
            if (type is not null)
                _navigationService.NavigateTo(type);
        }
    }

    private IEnumerable<NavigationViewItem> GetAllNavigationItems()
    {
        if (_navigationView is null) yield break;

        foreach (var item in _navigationView.MenuItems.OfType<NavigationViewItem>())
            yield return item;
        foreach (var item in _navigationView.FooterMenuItems.OfType<NavigationViewItem>())
            yield return item;
    }

    private static Type? GetNavigateToType(NavigationViewItem item)
    {
        var typeStr = item.GetValue(NavigationHelper.NavigateToProperty) as string;
        return typeStr is null ? null : Type.GetType(typeStr);
    }
}
