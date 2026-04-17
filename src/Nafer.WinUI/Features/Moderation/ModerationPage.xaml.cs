using Microsoft.UI.Xaml.Controls;

namespace Nafer.WinUI.Features.Moderation;

public sealed partial class ModerationPage : Page
{
    public ModerationViewModel? ViewModel { get; set; }

    public ModerationPage()
    {
        InitializeComponent();
    }
}
