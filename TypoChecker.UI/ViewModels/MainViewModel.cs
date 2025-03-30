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

namespace TypoChecker.UI.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private string input;

    [ObservableProperty]
    private ObservableCollection<ResultItem> results = new ObservableCollection<ResultItem>();

    [ObservableProperty]
    private double progress;

    public MainViewModel()
    {
        Options = GlobalOptions.LoadOrCreate();
        WeakReferenceMessenger.Default.Register(this, (object _, AppExitMessage m) =>
        {
            Options.Save();
        });
        CancelCheckCommand = CheckCommand.CreateCancelCommand();
    }

    public GlobalOptions Options { get; }

    public ICommand CancelCheckCommand { get; }


    [RelayCommand(IncludeCancelCommand = true)]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        Results = new ObservableCollection<ResultItem>();
        try
        {
            TypoCheckerCore service = new TypoCheckerCore();
            var progress = new Progress<double>(p => Progress = p);
            await foreach (var item in service.CheckAsync(Input, Options, progress, cancellationToken))
            {
                Results.Add(item);
            }
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
}
