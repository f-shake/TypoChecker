using System.Collections.Generic;
using TypoChecker.Models;

namespace TypoChecker.UI.Messages
{
    public class GenerateTypoInlinesMessage(List<TypoSegment> segments)
    {
        public List<TypoSegment> Segments { get; } = segments;
    }
}
