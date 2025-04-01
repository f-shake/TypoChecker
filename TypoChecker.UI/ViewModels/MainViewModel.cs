using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using TypoChecker.Models;
using TypoChecker.Options;
using TypoChecker.UI.Messages;
using TypoChecker.UI.Views;

namespace TypoChecker.UI.ViewModels;
public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string input;

    [ObservableProperty]
    private ObservableCollection<ParseFailedItem> otherOutputs = new ObservableCollection<ParseFailedItem>();

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private ObservableCollection<PromptItem> prompts = new ObservableCollection<PromptItem>();

    [ObservableProperty]
    private ObservableCollection<TypoItem> results = new ObservableCollection<TypoItem>();

    public MainViewModel()
    {
        CancelCheckCommand = CheckCommand.CreateCancelCommand();
    }

    public ICommand CancelCheckCommand { get; }

    // public GlobalOptions Options { get; }

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Input))
        {
            await WeakReferenceMessenger.Default.Send(new CommonDialogMessage
            {
                Type = CommonDialogMessage.CommonDialogType.Error,
                Title = "错误",
                Message = "输入内容为空"
            }).Task;
            return;
        }
        Results = new ObservableCollection<TypoItem>();
        Prompts = new ObservableCollection<PromptItem>();
        OtherOutputs = new ObservableCollection<ParseFailedItem>();
        try
        {
            TypoCheckerCore service = new TypoCheckerCore();
            var progress = new Progress<double>(p => Progress = p);
            var options = GlobalOptions.LoadOrCreate();
            await foreach (var item in service.CheckAsync(Input, options, progress, cancellationToken))
            {
                switch (item)
                {
                    case TypoItem t:
                        Results.Add(t);
                        break;
                    case ParseFailedItem f:
                        OtherOutputs.Add(f);
                        break;
                    case PromptItem p:
                        Prompts.Add(p);
                        break;
                    default:
                        break;
                }
            }

            var segments = TypoCheckerCore.SegmentTypos(Input, Results);
            WeakReferenceMessenger.Default.Send(new GenerateTypoInlinesMessage(segments));
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await WeakReferenceMessenger.Default.Send(new CommonDialogMessage
            {
                Type = CommonDialogMessage.CommonDialogType.Error,
                Exception = ex,
                Title = "检查失败"
            }).Task;
            return;
        }
        finally
        {
            Progress = 1d;
        }
    }

    [RelayCommand]
    private Task CopyValueAsync(string value)
    {
        return WeakReferenceMessenger.Default.Send(new GetClipboardMessage()).Clipboard.SetTextAsync(value);
    }

    [RelayCommand]
    private async Task SettingAsync()
    {
        var dialog = new ConfigDialog();
        await WeakReferenceMessenger.Default.Send(new DialogHostMessage(dialog)).Task;
    }
}
