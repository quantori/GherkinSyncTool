using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class TestCaseRequest
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public string Precondition { get; set; }
        public string ExpectedResult { get; set; }
        public bool? Deleted { get; set; }
        public bool? Automated { get; set; }
        public bool? External { get; set; }
        public int? TestLayerId { get; set; }
        public long? StatusId { get; set; }
        public int? WorkflowId { get; set; }
        public Scenario Scenario { get; set; }
        public List<Tag> Tags { get; set; }
        public List<Link> Links { get; set; }
        public List<CustomFieldItem> CustomFields { get; set; }
        public List<Member> Members { get; set; }
    }
}