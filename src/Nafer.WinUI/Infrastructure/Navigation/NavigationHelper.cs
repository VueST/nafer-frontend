using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Nafer.WinUI.Infrastructure.Navigation;

/// <summary>
/// Attached property that stores the ViewModel type name on a NavigationViewItem.
/// Used by NavigationViewService to resolve which ViewModel to navigate to.
/// </summary>
public static class NavigationHelper
{
    public static readonly DependencyProperty NavigateToProperty =
        DependencyProperty.RegisterAttached(
            "NavigateTo",
            typeof(string),
            typeof(NavigationHelper),
            new PropertyMetadata(null));

    public static string GetNavigateTo(NavigationViewItem item) =>
        (string)item.GetValue(NavigateToProperty);

    public static void SetNavigateTo(NavigationViewItem item, string value) =>
        item.SetValue(NavigateToProperty, value);
}
