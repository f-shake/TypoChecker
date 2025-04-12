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
[JsonSourceGenerationOptions(WriteIndented = true,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal partial class TypoCheckerJsonContext : JsonSerializerContext
{
    private static TypoCheckerJsonContext config;

    private static TypoCheckerJsonContext web;

    static TypoCheckerJsonContext()
    {
        web= new TypoCheckerJsonContext(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }); 
        config= new TypoCheckerJsonContext(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true
        });
    }
    public static TypoCheckerJsonContext Config => config;
    public static TypoCheckerJsonContext Web => web;
}
