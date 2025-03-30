namespace TypoChecker.Models;

public class OllamaRequestData
{
    public string Model { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public bool Stream { get; set; } = false;
}
