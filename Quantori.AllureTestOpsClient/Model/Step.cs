using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class Step
    {
        public string Name { get; set; }
        public string Keyword { get; set; }
        public List<Attachment> Attachments { get; set; }
        public bool Leaf { get; set; }
        public int StepsCount { get; set; }
        public bool HasContent { get; set; }
    }
}