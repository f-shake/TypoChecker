using CommunityToolkit.Mvvm.ComponentModel;
using TypoChecker.Options;

namespace TypoChecker.UI.ViewModels;

public partial class ConfigDialogViewModel : ViewModelBase
{
    [ObservableProperty]
    private GlobalOptions options = GlobalOptions.LoadOrCreate();

    [ObservableProperty]
    private bool useOllama;

    [ObservableProperty]
    private bool useOpenAi;

    public ConfigDialogViewModel()
    {
        if (Options.SourceType == SourceType.OpenAI)
        {
            UseOpenAi = true;
        }
        else
        {
            UseOllama = true;
        }
    }
    public void Save()
    {
        Options.Save();
    }

    partial void OnUseOllamaChanged(bool value)
    {
        if (value)
        {
            Options.SourceType = SourceType.Ollama;
        }
    }

    partial void OnUseOpenAiChanged(bool value)
    {
        if (value)
        {
            Options.SourceType = SourceType.OpenAI;
        }
    }
}