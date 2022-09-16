using System.Collections.Generic;
using Quantori.AllureTestOpsClient.Model;
using Refit;

namespace GherkinSyncTool.Synchronizers.AllureTestOps.Model
{
    public class CreateTestCaseRequestExtended
    {
        public CreateTestCaseRequest CreateTestCaseRequest { get; set; } = new();
        
        /// <summary>
        /// Dictionary consists of step number and attachment.
        /// </summary>
        public Dictionary<int, ByteArrayPart> StepsAttachments { get; set; } = new();
    }
}