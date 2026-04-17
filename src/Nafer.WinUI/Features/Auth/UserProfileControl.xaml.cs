using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Nafer.Core.Application.ViewModels.Auth;

namespace Nafer.WinUI.Features.Auth;

public sealed partial class UserProfileControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(AccountViewModel),
            typeof(UserProfileControl),
            new PropertyMetadata(null));

    public AccountViewModel? ViewModel
    {
        get => (AccountViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public UserProfileControl()
    {
        InitializeComponent();
    }

    public Microsoft.UI.Xaml.Media.SolidColorBrush GetBadgeBackgroundColor(Nafer.Core.Domain.Models.UserRole role)
    {
        return role switch
        {
            Nafer.Core.Domain.Models.UserRole.Admin => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 59, 48)),   // 15% Solid Red
            Nafer.Core.Domain.Models.UserRole.Mod => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 0, 122, 255)),   // 15% Solid Blue
            Nafer.Core.Domain.Models.UserRole.Premium => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 255, 149, 0)), // 15% Solid Orange
            _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(40, 142, 142, 147)) // 15% Solid Grey
        };
    }

    public Microsoft.UI.Xaml.Media.SolidColorBrush GetBadgeForegroundColor(Nafer.Core.Domain.Models.UserRole role)
    {
        return role switch
        {
            Nafer.Core.Domain.Models.UserRole.Admin => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 59, 48)),
            Nafer.Core.Domain.Models.UserRole.Mod => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 122, 255)),
            Nafer.Core.Domain.Models.UserRole.Premium => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 149, 0)),
            _ => new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 142, 142, 147))
        };
    }
}
