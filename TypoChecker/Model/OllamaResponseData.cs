using System.Text.Json.Serialization;

namespace TypoChecker.Models;

public class OllamaResponseData
{
    [JsonPropertyName("response")]
    public string Response { get; set; } = string.Empty;
}
