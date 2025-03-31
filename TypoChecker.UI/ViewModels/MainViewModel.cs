﻿using Avalonia;
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
    private ObservableCollection<ParseFailedItem> otherOutputs = new ObservableCollection<ParseFailedItem>();

    [ObservableProperty]
    private double progress;

    [ObservableProperty]
    private ObservableCollection<PromptItem> prompts = new ObservableCollection<PromptItem>();

    [ObservableProperty]
    private ObservableCollection<TypoItem> results = new ObservableCollection<TypoItem>();

    public MainViewModel()
    {
        Options = GlobalOptions.LoadOrCreate();
        WeakReferenceMessenger.Default.Register(this, (object _, AppExitMessage m) =>
        {
            Options.Save();
        });
        CancelCheckCommand = CheckCommand.CreateCancelCommand();
    }

    public ICommand CancelCheckCommand { get; }
    public GlobalOptions Options { get; }
    [RelayCommand(IncludeCancelCommand = true)]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        Results = new ObservableCollection<TypoItem>();
        Prompts = new ObservableCollection<PromptItem>();
        OtherOutputs = new ObservableCollection<ParseFailedItem>();
        try
        {
            TypoCheckerCore service = new TypoCheckerCore();
            var progress = new Progress<double>(p => Progress = p);
            await foreach (var item in service.CheckAsync(Input, Options, progress, cancellationToken))
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
