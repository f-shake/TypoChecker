using System.Text.Json;
using OpenAI;
using System.ClientModel;
using TypoChecker.Options;
using TypoChecker.Models;

public class TypoCheckerCore
{
    public async IAsyncEnumerable<ResultItem> CheckAsync(string text, GlobalOptions config, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var segments = SplitText(text, config.MinSegmentLength);
        for (int index = 0; index < segments.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string segment = segments[index];
            progress?.Report((double)(0 + index) / segments.Count);

            string prompt = config.Prompt + segment;
            string result = config.SourceType switch
            {
                SourceType.OpenAI => await CallOpenAI(prompt, config.OpenAiOptions),
                SourceType.Ollama => await CallOllamaApi(prompt, config.OllamaOptions),
                _ => throw new NotImplementedException()
            };

            var lines = result.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            int startLine = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i] == "</think>")
                {
                    startLine = i + 1;
                    break;
                }
            }
            for (int i = startLine; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line == config.EmptyOutput)
                {
                    continue;
                }
                yield return Parse(line);
            }
        }
    }

    private async Task<string> CallOllamaApi(string prompt, OllamaOptions config)
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

    private async Task<string> CallOpenAI(string text, OpenAiOptions config)
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

    private ResultItem Parse(string text)
    {
        string[] parts = text.Trim().Split(['|'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 4)
        {
            return new ResultItem { Message = "格式错误：" + text };
        }

        return new ResultItem
        {
            Sentense = parts[0],
            WrongWords = parts[1],
            CorrectWords = parts[2],
            Possibility = parts[3]
        };
    }

    private List<string> SplitText(string text, int minSegmentLength)
    {
        char[] delimiters = { '。', '？', '！', '\n', '\r' };

        List<string> segments = text.Split(delimiters)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Where(p => p != "")
            .ToList();

        List<string> finalSegments = new();
        string temp = "";

        foreach (var seg in segments)
        {
            if (temp.Length + seg.Length < minSegmentLength)
            {
                temp += seg;
            }
            else
            {
                if (!string.IsNullOrEmpty(temp)) finalSegments.Add(temp);
                temp = seg;
            }
        }

        if (!string.IsNullOrEmpty(temp)) finalSegments.Add(temp);

        return finalSegments;
    }
}
