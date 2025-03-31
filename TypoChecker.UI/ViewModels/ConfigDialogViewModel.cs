using CommunityToolkit.Mvvm.ComponentModel;
using TypoChecker.Options;

namespace TypoChecker.UI.ViewModels;

public partial class ConfigDialogViewModel : ViewModelBase
{
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

    [ObservableProperty]
    private GlobalOptions options = GlobalOptions.LoadOrCreate();

    [ObservableProperty]
    private bool useOpenAi;

    [ObservableProperty]
    private bool useOllama;

    partial void OnUseOpenAiChanged(bool value)
    {
        if (value)
        {
            Options.SourceType = SourceType.OpenAI;
        }
    }

    partial void OnUseOllamaChanged(bool value)
    {
        if (value)
        {
            Options.SourceType = SourceType.Ollama;
        }
    }

    public void Save()
    {
        Options.Save();
    }
}