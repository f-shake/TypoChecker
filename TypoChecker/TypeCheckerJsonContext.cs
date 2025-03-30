using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Unicode;
using TypoChecker.Models;
using TypoChecker.Options;

[JsonSerializable(typeof(OllamaRequestData))]
[JsonSerializable(typeof(OllamaOptions))]
[JsonSerializable(typeof(OllamaResponseData))]
[JsonSerializable(typeof(OpenAiOptions))]
[JsonSerializable(typeof(GlobalOptions))]
internal partial class TypeCheckerJsonContext : JsonSerializerContext
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    }; 
    
    public static TypeCheckerJsonContext Instance { get; } = new TypeCheckerJsonContext(jsonSerializerOptions);
}
