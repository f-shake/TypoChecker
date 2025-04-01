//#define USE_DEFAULT

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

    public string Prompt { get; set; } = """
        请你帮我检查文段中的错误。需要检查的内容包括：1.错别字（包括中文和英文等）；2.标点符号（误用、中文语段用了英文标点、英文语段用了中文标点）。

        输出要求：
        1. 每个错误一行，以Markdown表格的形式，但不加表头，格式：
        |错误位置前后5-10字|原词|修正词|修正后的语段|说明|
        其中第四列，与第一列的语段相同，但应该是正确的版本，是你修正后的文段。
        如果该段话中有多个错别字，应当全部进行修正。
        2. 不需要对文本进行优化，仅指出明显错误的地方
        3. 以下情况豁免：网络用语、标注方言、代码/专有名词

        示例：
        输入："新iphone很贵，但 销量很好，她笑的很开心，因为他是销售经历。"
        输出：
        |新iphone很贵|iphone|iPhone|新iPhone很贵|品牌大小写错误|
        |但 销量很好|（空格）|（无空格）|但销量很好|中文之间不该有空格|
        |她笑的很开心|的|得|她笑得很开心|动词在前，应使用“得”|
        |因为他是销售经历|他|她|因为她是销售经理|前后代词不一致|
        |因为他是销售经历|经历|经理|因为她是销售经理|错别字|
        
        有错误时，只输出上文要求的错误内容，不输出其他内容；
        无错误时输出："无错误"（不包括引号）

        以下是待检查内容：
        """;

    public string EmptyOutput = "无错别字";


    public static GlobalOptions LoadOrCreate()
    {
#if !DEBUG || !USE_DEFAULT
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize(json, TypeCheckerJsonContext.Instance.GlobalOptions) ?? new GlobalOptions();
        }
#endif
        return new GlobalOptions();
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(this, TypeCheckerJsonContext.Instance.GlobalOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
