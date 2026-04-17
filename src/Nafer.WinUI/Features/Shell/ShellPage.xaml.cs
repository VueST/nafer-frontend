using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Nafer.WinUI.Infrastructure.Navigation;

namespace Nafer.WinUI.Features.Shell;

public sealed partial class ShellPage : Page
{
    public ShellViewModel ViewModel { get; }

    public ShellPage()
    {
        ViewModel = App.GetService<ShellViewModel>();
        InitializeComponent();

        var navService = (NavigationService)ViewModel.NavigationService;
        navService.Frame = NavigationFrame;
        ViewModel.NavigationViewService.Initialize(NavigationViewControl);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        NavigationViewControl.IsPaneOpen = false;

        // Navigate to Home by default on first launch
        if (ViewModel.NavigationService.Frame.Content is null)
        {
            ViewModel.NavigationService.NavigateTo<Nafer.WinUI.Features.Home.HomeViewModel>();
        }
    }

    public void TogglePane() => NavigationViewControl.IsPaneOpen = !NavigationViewControl.IsPaneOpen;

    private DateTimeOffset _lastClosedTime;

    private void AccountNavItem_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var element = (FrameworkElement)sender;
        var flyout = FlyoutBase.GetAttachedFlyout(element);
        
        if (flyout == null) return;

        // Debounce: If the flyout was closed in the last 200ms, ignore this tap.
        // Taps on the button while a flyout is open cause a 'LightDismiss' (Close) 
        // immediately followed by the Tap event.
        if (DateTimeOffset.Now - _lastClosedTime < TimeSpan.FromMilliseconds(200))
        {
            return;
        }

        // Set pass-through to the highest possible element (the Window root) 
        // to allow clicking top-bar buttons while the flyout is open.
        if (flyout is Flyout f && f.OverlayInputPassThroughElement == null)
        {
            f.OverlayInputPassThroughElement = this.XamlRoot?.Content;
        }

        flyout.Opened -= OnFlyoutOpened;
        flyout.Closed -= OnFlyoutClosed;
        flyout.Opened += OnFlyoutOpened;
        flyout.Closed += OnFlyoutClosed;

        FlyoutBase.ShowAttachedFlyout(element);
    }

    private void OnFlyoutOpened(object? sender, object e) { }
    
    private void OnFlyoutClosed(object? sender, object e)
    {
        _lastClosedTime = DateTimeOffset.Now;
    }
}
