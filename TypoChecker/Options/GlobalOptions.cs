using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace TypoChecker.Options;
public class GlobalOptions
{
    public OllamaOptions OllamaOptions { get; set; } = new OllamaOptions();
    public OpenAiOptions OpenAiOptions { get; set; } = new OpenAiOptions();
    public SourceType SourceType { get; set; } = SourceType.Ollama;
    public int MinSegmentLength { get; set; } = 100;

    private static readonly string ConfigPath = "config.json";

    public string Prompt { get; set; } = $"帮我检查错别字和标点错误，只检查这两项，不要检查其他内容。" +
        $"如果存在任何错别字，每个错别字/词输出一行，输出一张表格，按以下格式：" +
        $"第一列为所在语段，第二列为原词（字/标点），第三列为修改后的字（词/标点），第四列为错误的可能性，第五列为相关信息，比如为什么要修改。" +
        $"不要输出表头。" +
        $"第一列部分，只需要前后10个字左右。" +
        $"例如，输入“今天我很高心，妈妈做了好吃的”，输出“|今天我很高心|心|兴|95%|错别字|”。" +
        $"不要对语句进行优化，只需要提供明显错误的地方，例如，不要提出口语化、简化等建议。" +
        $"如果没有错别字，就不要输出表格，直接输出“无错别字”。" +
        $"以下是需要检查的内容：";

    public string EmptyOutput = "无错别字";


    public static GlobalOptions LoadOrCreate()
    {
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize(json, TypeCheckerJsonContext.Instance.GlobalOptions) ?? new GlobalOptions();
        }
        return new GlobalOptions();
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(this, TypeCheckerJsonContext.Instance.GlobalOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
