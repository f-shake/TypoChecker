using System.Text.Json;
using OpenAI;
using System.ClientModel;
using TypoChecker.Options;
using TypoChecker.Models;

namespace TypoChecker;

internal static class LlmCaller
{
    public static async Task<string> CallOllamaApi(string prompt, OllamaOptions config)
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(1000) };

        var requestData = new OllamaRequestData { Model = config.Model, Prompt = prompt, Stream = false };

        string jsonContent = JsonSerializer.Serialize(requestData, TypeCheckerJsonContext.Default.OllamaRequestData);

        HttpContent content = new StringContent(jsonContent);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize(jsonResponse, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<OllamaResponseData>)TypeCheckerJsonContext.Default.OllamaResponseData);

        return responseObject?.Response ?? "";
    }

    public static async Task<string> CallOpenAI(string text, OpenAiOptions config)
    {
        if (string.IsNullOrEmpty(config.Key))
        {
            throw new FileNotFoundException($"Key为空");
        }

        if (string.IsNullOrEmpty(config.Model))
        {
            throw new FileNotFoundException($"没有指定模型");
        }

        OpenAIClient client = new OpenAIClient(
            new ApiKeyCredential(config.Key),
            new OpenAIClientOptions
            {
                Endpoint = new Uri(config.Url),
            });
        var result = await client.GetChatClient(config.Model).CompleteChatAsync(text);
        return string.Join(Environment.NewLine, result.Value.Content.Select(p => p.Text));
    }

}
