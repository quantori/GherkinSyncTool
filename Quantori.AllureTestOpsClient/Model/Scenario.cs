using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class Scenario
    {
        public int TestResultId { get; set; }
        public List<Attachment> Attachments { get; set; }
        public List<Step> Steps { get; set; }
    }
}