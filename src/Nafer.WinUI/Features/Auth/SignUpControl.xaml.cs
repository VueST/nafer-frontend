using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Nafer.Core.Application.ViewModels.Auth;

namespace Nafer.WinUI.Features.Auth;

public sealed partial class SignUpControl : UserControl
{
    public static readonly DependencyProperty ViewModelProperty =
        DependencyProperty.Register(
            nameof(ViewModel),
            typeof(RegisterViewModel),
            typeof(SignUpControl),
            new PropertyMetadata(null));

    public RegisterViewModel? ViewModel
    {
        get => (RegisterViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public SignUpControl()
    {
        InitializeComponent();
    }
}
