using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using TypoChecker.UI.Messages;

namespace TypoChecker.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        this.RegisterCommonDialogMessage();
        this.RegisterGetClipboardMessage();
        this.RegisterDialogHostMessage();
        RegisterTypoInlinesMessage();
    }

    private void RegisterTypoInlinesMessage()
    {
        WeakReferenceMessenger.Default.Register<GenerateTypoInlinesMessage>(this, (_, m) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                tbkResults.Inlines.Clear();
                foreach (var seg in m.Segments)
                {
                    var run1 = new Run(seg.Text);
                    var run2 = new Run(seg.Text);
                    if (seg.HasTypo)
                    {
                        run1.Foreground = Brushes.Red;
                        if (seg.Typo != null)
                        {
                            run2.Foreground = Brushes.Green;
                            run2.Text = seg.Typo.CorrectSentense;
                            //ToolTip.SetTip(run, $"{seg.Typo.Message}，建议修改为：{seg.Typo.CorrectSentense}");
                        }
                    }
                    tbkResults.Inlines.Add(run1);
                    tbkCorrect.Inlines.Add(run2);
                }
            });
        });
    }
}
