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
        1. 以JSON数组格式返回结果，每个错误是一个对象，包含以下字段：
        {
          "context": "错误位置前后5-10字",
          "original": "原词",
          "corrected": "修正词",
          "fixed_segment": "修正后的语段",
          "explanation": "说明"
        }
        如果该段话中有多个错别字，应当全部进行修正。
        2. 不需要对文本进行优化，仅指出明显错误的地方
        3. 以下情况豁免：网络用语、标注方言、代码/专有名词

        示例输入："新iphone很贵，但 销量很好，她笑的很开心，因为他是销售经历。"
        示例输出：
        {
            "errors":
            [
              {
                "context": "新iphone很贵",
                "original": "iphone",
                "corrected": "iPhone",
                "fixed_segment": "新iPhone很贵",
                "explanation": "品牌大小写错误"
              },
              {
                "context": "但 销量很好",
                "original": "（空格）",
                "corrected": "（无空格）",
                "fixed_segment": "但销量很好",
                "explanation": "中文之间不该有空格"
              },
              {
                "context": "她笑的很开心",
                "original": "的",
                "corrected": "得",
                "fixed_segment": "她笑得很开心",
                "explanation": "动词在前，应使用'得'"
              },
              {
                "context": "因为他是销售经历",
                "original": "他",
                "corrected": "她",
                "fixed_segment": "因为她是销售经理",
                "explanation": "前后代词不一致"
              },
              {
                "context": "因为他是销售经历",
                "original": "经历",
                "corrected": "经理",
                "fixed_segment": "因为她是销售经理",
                "explanation": "错别字"
              }
            ]
        }
        有错误时，只输出上文要求的JSON格式的错误内容，不输出其他内容；
        无错误时输出一个空的JSON数组，即：{"errors": []}。


        请严格按上述JSON格式输出结果，不要包含任何额外的解释或说明。
        输出的格式应该严格遵从JSON格式，该转义的地方记得转义。
        """;

    public string EmptyOutput = "无错误";


    public static GlobalOptions LoadOrCreate()
    {
#if !DEBUG || !USE_DEFAULT
        if (File.Exists(ConfigPath))
        {
            string json = File.ReadAllText(ConfigPath);
            return JsonSerializer.Deserialize(json, TypoCheckerJsonContext.Config.GlobalOptions) ?? new GlobalOptions();
        }
#endif
        return new GlobalOptions();
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(this, TypoCheckerJsonContext.Config.GlobalOptions);
        File.WriteAllText(ConfigPath, json);
    }
}
