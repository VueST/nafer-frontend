using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Nafer.Core.Application.ViewModels.Auth;

namespace Nafer.WinUI.Features.Auth;

public sealed partial class SignInControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(LoginViewModel),
            typeof(SignInControl),
            new PropertyMetadata(null));

    public LoginViewModel? ViewModel
    {
        get => (LoginViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public SignInControl()
    {
        InitializeComponent();
    }
}
