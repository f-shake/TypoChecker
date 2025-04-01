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
    public double[] Ticks { get; } =
    {
        100, 200, 300, 400, 500, 600, 700, 800, 900,
        1000, 2000, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000
    };

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