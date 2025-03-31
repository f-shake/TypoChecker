using System.Text.Json;
using OpenAI;
using System.ClientModel;
using TypoChecker.Options;
using TypoChecker.Models;
using System.Text;
using System.Runtime.CompilerServices;

public class TypoCheckerCore
{
    public async IAsyncEnumerable<ICheckItem> CheckAsync(string text,
                                                         GlobalOptions config,
                                                         IProgress<double> progress,
                                                         [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var segments = SplitText(text, config.MinSegmentLength);
        for (int index = 0; index < segments.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string segment = segments[index];
            progress?.Report((double)(0 + index) / segments.Count);

            string prompt = config.Prompt + segment;
            yield return new PromptItem(prompt);
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
                ICheckItem c = null;
                try
                {
                  c= Parse(line);
                }
                catch(FormatException ex)
                {
                    c = new ParseFailedItem(ex.Message);
                }
                yield return c;
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

    private TypoItem Parse(string text)
    {
        string[] parts = text.Trim().Split(['|'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length != 5)
        {
            throw new FormatException("格式错误：" + text);
        }

        return new TypoItem
        {
            Sentense = parts[0],
            WrongWords = parts[1],
            CorrectWords = parts[2],
            Possibility = parts[3],
            Message = parts[4]
        };
    }

    private List<string> SplitText(string text, int minSegmentLength)
    {
        // 定义分隔符（保留原分隔符）
        char[] delimiters = ['。', '？', '！', '\n', '\r'];

        // 使用StringBuilder提高性能
        var finalSegments = new List<string>();
        var currentSegment = new StringBuilder();
        int lastDelimiterIndex = -1;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            currentSegment.Append(c);

            // 检查是否是分隔符
            if (delimiters.Contains(c))
            {
                // 如果是换行符，可能需要特殊处理
                if (c == '\n' || c == '\r')
                {
                    // 处理Windows风格的换行(\r\n)
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        currentSegment.Append('\n');
                        i++;
                    }
                }

                string segment = currentSegment.ToString();

                // 如果当前分段足够长，或者下一个字符是文本结束
                if (segment.Length >= minSegmentLength || i == text.Length - 1)
                {
                    finalSegments.Add(segment);
                    currentSegment.Clear();
                    lastDelimiterIndex = -1;
                }
                else
                {
                    lastDelimiterIndex = currentSegment.Length - 1;
                }
            }
            // 检查是否达到最小长度但未找到分隔符
            else if (currentSegment.Length >= minSegmentLength && lastDelimiterIndex >= 0)
            {
                // 在最后一个分隔符处分割
                string segment = currentSegment.ToString(0, lastDelimiterIndex + 1);
                finalSegments.Add(segment);

                // 保留分隔符后的内容
                string remaining = currentSegment.ToString(lastDelimiterIndex + 1, currentSegment.Length - lastDelimiterIndex - 1);
                currentSegment.Clear();
                currentSegment.Append(remaining);
                lastDelimiterIndex = -1;
            }
        }

        // 添加最后剩余的部分
        if (currentSegment.Length > 0)
        {
            finalSegments.Add(currentSegment.ToString());
        }

        // 过滤空段落
        return finalSegments.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
    }
}
