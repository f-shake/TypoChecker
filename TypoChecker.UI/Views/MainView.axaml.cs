using Avalonia.Controls;
using FzLib.Avalonia.Messages;

namespace TypoChecker.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.RegisterCommonDialogMessage();
        this.RegisterGetClipboardMessage();
    }
}
