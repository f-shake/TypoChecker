using TypoChecker.Options;
using TypoChecker.Models;
using System.Text;
using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;

namespace TypoChecker;
public class TypoCheckerCore
{
    public static List<TypoSegment> SegmentTypos(string rawText, IList<TypoItem> typos)
    {
        //AI的回复中，正确修正后语段，如果一句话中存在多个错误，只会在最后一个修正时全部修正正确。
        var temp = typos;
        typos = new List<TypoItem>();
        foreach (var t in temp)
        {
            if (typos.Count == 0 || typos[^1].Context != t.Context)
            {
                typos.Add(t);
            }
        }

        var segments = new List<TypoSegment>();

        if (string.IsNullOrEmpty(rawText))
        {
            return segments;
        }

        if (typos == null || typos.Count == 0)
        {
            segments.Add(new TypoSegment
            {
                Text = rawText,
                HasTypo = false,
                Typo = null
            });
            return segments;
        }

        // 先按句子长度降序排序，优先处理更长的匹配项
        var sortedTypos = typos
            .Where(t => !string.IsNullOrEmpty(t.Context))
            .OrderByDescending(t => t.Context.Length)
            .ToList();

        // 记录每个字符属于哪个错误句子(null表示不属于任何错误)
        TypoItem[] charTypoMap = new TypoItem[rawText.Length];

        // 遍历所有错误句子
        foreach (var typo in sortedTypos)
        {
            int index = 0;
            while (index < rawText.Length)
            {
                int foundIndex = rawText.IndexOf(typo.Context, index, StringComparison.Ordinal);
                if (foundIndex == -1)
                    break;

                // 检查这个匹配是否完全未被标记
                bool canMark = true;
                for (int i = foundIndex; i < foundIndex + typo.Context.Length; i++)
                {
                    if (charTypoMap[i] != null)
                    {
                        canMark = false;
                        break;
                    }
                }

                if (canMark)
                {
                    // 标记这部分为当前错误
                    for (int i = foundIndex; i < foundIndex + typo.Context.Length; i++)
                    {
                        charTypoMap[i] = typo;
                    }
                }

                index = foundIndex + typo.Context.Length;
            }
        }

        // 现在根据标记构建分段
        int currentPos = 0;
        while (currentPos < rawText.Length)
        {
            // 处理非错误文本
            if (charTypoMap[currentPos] == null)
            {
                int start = currentPos;
                while (currentPos < rawText.Length && charTypoMap[currentPos] == null)
                {
                    currentPos++;
                }
                segments.Add(new TypoSegment
                {
                    Text = rawText.Substring(start, currentPos - start),
                    HasTypo = false,
                    Typo = null
                });
                continue;
            }

            // 处理错误文本
            TypoItem currentTypo = charTypoMap[currentPos];
            int typoStart = currentPos;

            // 找到连续的相同错误类型的文本
            while (currentPos < rawText.Length && charTypoMap[currentPos] == currentTypo)
            {
                currentPos++;
            }

            segments.Add(new TypoSegment
            {
                Text = rawText.Substring(typoStart, currentPos - typoStart),
                HasTypo = true,
                Typo = currentTypo
            });
        }

        return segments;
    }

    public async IAsyncEnumerable<ICheckItem> CheckAsync(string text,
                                                             GlobalOptions config,
                                                         IProgress<double> progress,
                                                         [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        var segments = SplitText(text, config.MinSegmentLength);
        for (int index = 0; index < segments.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string segment = segments[index];
            progress?.Report((double)(0 + index) / segments.Count);

            yield return new PromptItem(segment);
            string result = config.SourceType switch
            {
                SourceType.OpenAI => await LlmCaller.CallOpenAI(config.Prompt, segment, config.OpenAiOptions),
                SourceType.Ollama => await LlmCaller.CallOllamaApi(config.Prompt, segment, config.OllamaOptions),
                _ => throw new NotImplementedException()
            };

            yield return new OutputItem(result);
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
            if (startLine > 0)
            {
                result = string.Join(Environment.NewLine, lines[startLine..]);
            }
            IEnumerable<TypoItem> results = [];
            try
            {
                results = Parse(result).ToList();
            }
            catch (FormatException ex)
            {
                continue;
            }
            foreach (var r in results)
            {
                yield return r;
            }
        }
    }

    private IEnumerable<TypoItem> Parse(string text)
    {
        if (!JsonNode.Parse(text).AsObject().TryGetPropertyValue("errors", out var errors))
        {
            throw new FormatException("输出内容中找不到errors对象");
        }
        if (errors is not JsonArray errorArray)
        {
            throw new FormatException("输出内容中的errors不是数组");
        }
        foreach (var error in errorArray)
        {
            if (error is not JsonObject errorObj)
            {
                throw new FormatException("输出内容中的errors数组元素不是对象");
            }
            if (!errorObj.TryGetPropertyValue("context", out var context) ||
                !errorObj.TryGetPropertyValue("original", out var original) ||
                !errorObj.TryGetPropertyValue("corrected", out var corrected) ||
                !errorObj.TryGetPropertyValue("fixed_segment", out var fixedSegment) ||
                !errorObj.TryGetPropertyValue("explanation", out var explanation))
            {
                throw new FormatException("输出内容中的errors对象缺少必要字段");
            }
            yield return new TypoItem
            {
                Context = context.ToString(),
                Original = original.ToString(),
                Corrected = corrected.ToString(),
                FixedSegment = fixedSegment.ToString(),
                Explanation = explanation.ToString()
            };
        }
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