using System.Text.Json;
using OpenAI;
using System.ClientModel;
using TypoChecker.Options;
using TypoChecker.Models;
using OpenAI.Chat;

namespace TypoChecker;

internal static class LlmCaller
{
    public static async Task<string> CallOllamaApi(string systemPrompt, string input, OllamaOptions config)
    {
        using HttpClient client = new() { Timeout = TimeSpan.FromSeconds(1000) };

        var requestData = new OllamaRequestData
        {
            //Messages = [new OllamaRequestData.OllamaMessage
            //    {
            //        Role = "system",
            //        Content = systemPrompt,
            //    },
            //    new OllamaRequestData.OllamaMessage
            //    {
            //        Role = "user",
            //        Content = input
            //    }
            //],
            Stream = false,
            Model = config.Model,
            Prompt = systemPrompt + Environment.NewLine + input
        };

        string jsonContent = JsonSerializer.Serialize(requestData, TypoCheckerJsonContext.Web.OllamaRequestData);

        HttpContent content = new StringContent(jsonContent);
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        HttpResponseMessage response = await client.PostAsync(config.Url, content);
        response.EnsureSuccessStatusCode();

        string jsonResponse = await response.Content.ReadAsStringAsync();
        var responseObject = JsonSerializer.Deserialize(jsonResponse, (System.Text.Json.Serialization.Metadata.JsonTypeInfo<OllamaResponseData>)TypoCheckerJsonContext.Web.OllamaResponseData);

        return responseObject?.Response ?? """{"errors":[]}""";
    }

    public static async Task<string> CallOpenAI(string systemPrompt, string input, OpenAiOptions config)
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

        ChatCompletionOptions cco = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat(),
        };

        var systemMessage = ChatMessage.CreateSystemMessage(systemPrompt);
        var userMessage = ChatMessage.CreateUserMessage(input);

        var result = await client.GetChatClient(config.Model).CompleteChatAsync([systemMessage, userMessage], cco);
        return string.Join(Environment.NewLine, result.Value.Content.Select(p => p.Text));
    }

}
