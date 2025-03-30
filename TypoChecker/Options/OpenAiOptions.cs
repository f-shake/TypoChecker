namespace TypoChecker.Options;

public class OpenAiOptions
{
    public string Model { get; set; }
    public string Url { get; set; } = "https://api.deepseek.com";
    public string Key { get; set; }
}
