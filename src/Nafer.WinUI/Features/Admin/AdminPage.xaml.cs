using Microsoft.UI.Xaml.Controls;

namespace Nafer.WinUI.Features.Admin;

public sealed partial class AdminPage : Page
{
    public AdminViewModel? ViewModel { get; set; }

    public AdminPage()
    {
        InitializeComponent();
    }
}
