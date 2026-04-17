using Nafer.Core.Application.ViewModels.Auth;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
namespace Nafer.WinUI.Features.Auth;

public sealed partial class AccountFlyoutControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(AccountViewModel),
            typeof(AccountFlyoutControl),
            new PropertyMetadata(null, OnViewModelChanged));

    public AccountViewModel? ViewModel
    {
        get => (AccountViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    private static void OnViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AccountFlyoutControl control && e.NewValue is AccountViewModel vm)
        {
            control.SignInView.ViewModel = vm.Login;
            control.SignUpView.ViewModel = vm.Register;
            control.ProfileView.ViewModel = vm;
        }
    }

    public AccountFlyoutControl()
    {
        InitializeComponent();
        
        if (AuthSelectorBar != null)
        {
            AuthSelectorBar.SelectedItem = AuthSelectorBar.Items[0];
        }
    }

    private void AuthSelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (SignInView == null || SignUpView == null) return;

        if (sender.SelectedItem is SelectorBarItem item)
        {
            if (item.Text == "Sign In")
            {
                SignUpView.Visibility = Visibility.Collapsed;
                AnimateEntrance(SignInView, -20f);
            }
            else
            {
                SignInView.Visibility = Visibility.Collapsed;
                AnimateEntrance(SignUpView, 20f);
            }
        }
    }

    private void AnimateEntrance(UIElement element, float offsetX)
    {
        element.Visibility = Visibility.Visible;
        
        var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        // Set initial state
        visual.Offset = new System.Numerics.Vector3(offsetX, 0, 0);
        visual.Opacity = 0.0f;

        // Translation Animation
        var offsetAnimation = compositor.CreateVector3KeyFrameAnimation();
        offsetAnimation.InsertKeyFrame(1.0f, new System.Numerics.Vector3(0, 0, 0));
        offsetAnimation.Duration = TimeSpan.FromMilliseconds(400);

        // Opacity Animation
        var opacityAnimation = compositor.CreateScalarKeyFrameAnimation();
        opacityAnimation.InsertKeyFrame(1.0f, 1.0f);
        opacityAnimation.Duration = TimeSpan.FromMilliseconds(400);

        visual.StartAnimation("Offset", offsetAnimation);
        visual.StartAnimation("Opacity", opacityAnimation);
    }
}
