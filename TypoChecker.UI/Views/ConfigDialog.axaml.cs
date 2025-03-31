using Avalonia.Controls;
using FzLib.Avalonia.Dialogs;
using FzLib.Avalonia.Messages;
using TypoChecker.UI.ViewModels;

namespace TypoChecker.UI.Views;

public partial class ConfigDialog : DialogHost
{
    public ConfigDialog()
    {
        DataContext = new ConfigDialogViewModel();
        InitializeComponent();
    }

    protected override void OnCloseButtonClick()
    {
        Close();
    }

    protected override void OnPrimaryButtonClick()
    {
        ((ConfigDialogViewModel)DataContext).Save();
        Close();
    }
}
