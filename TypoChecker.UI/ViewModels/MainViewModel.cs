using Avalonia;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FzLib.Avalonia.Messages;
using System;
using System.Collections.Generic;
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
    private ObservableCollection<OutputItem> outputs = new ObservableCollection<OutputItem>();

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

    [RelayCommand(IncludeCancelCommand = true)]
    private async Task CheckAsync(CancellationToken cancellationToken)
    {
        var options = GlobalOptions.LoadOrCreate();
        if (string.IsNullOrWhiteSpace(Input))
        {
            await ShowErrorAsync("输入内容为空", "错误");
            return;
        }

        var errors = new List<string>();
        string errorTitle = null;

        switch (options.SourceType)
        {
            case SourceType.OpenAI:
                errorTitle = "OpenAI 配置错误";
                ValidateNotEmpty(options.OpenAiOptions.Key, "OpenAI API Key为空，请先设置", errors);
                ValidateNotEmpty(options.OpenAiOptions.Model, "OpenAI 模型为空，请先设置", errors);
                ValidateNotEmpty(options.OpenAiOptions.Url, "OpenAI 地址为空，请先设置", errors);
                break;

            case SourceType.Ollama:
                errorTitle = "Ollama 配置错误";
                ValidateNotEmpty(options.OllamaOptions.Model, "Ollama API 模型名称为空，请先设置", errors);
                ValidateNotEmpty(options.OllamaOptions.Url, "Ollama 地址为空，请先设置", errors);
                break;
        }

        if (errors.Count > 0)
        {
            await ShowErrorAsync(string.Join("\n", errors), errorTitle);
            await SettingAsync();
            return;
        }
        await CheckCoreAsync(options, cancellationToken);

        void ValidateNotEmpty(string value, string errorMessage, List<string> errorList)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errorList.Add(errorMessage);
            }
        }
    }

    private async Task CheckCoreAsync(GlobalOptions options, CancellationToken cancellationToken)
    {
        Results = new ObservableCollection<TypoItem>();
        Prompts = new ObservableCollection<PromptItem>();
        Outputs = new ObservableCollection<OutputItem>();
        WeakReferenceMessenger.Default.Send(new GenerateTypoInlinesMessage([]));

        try
        {
            TypoCheckerCore service = new TypoCheckerCore();
            var progress = new Progress<double>(p => Progress = p);
            await foreach (var item in service.CheckAsync(Input, options, progress, cancellationToken))
            {
                switch (item)
                {
                    case TypoItem t:
                        Results.Add(t);
                        break;
                    case OutputItem f:
                        Outputs.Add(f);
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

            if (Results.Count == 0)
            {
                await WeakReferenceMessenger.Default.Send(new CommonDialogMessage
                {
                    Type = CommonDialogMessage.CommonDialogType.Ok,
                    Title = "检查完成",
                    Message = "未检查出错误",
                }).Task;
                return;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            await ShowErrorAsync(ex, "检查失败");
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

    private Task ShowErrorAsync(object exOrMsg, string title)
    {
        return WeakReferenceMessenger.Default.Send(new CommonDialogMessage
        {
            Type = CommonDialogMessage.CommonDialogType.Error,
            Title = title,
            Exception = exOrMsg as Exception,
            Message = exOrMsg as string,
        }).Task;
    }
}
