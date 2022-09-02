using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class TestCase : Item
    {
        public int ProjectId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public string DescriptionHtml { get; set; }
        public string Precondition { get; set; }
        public string PreconditionHtml { get; set; }
        public string ExpectedResult { get; set; }
        public string ExpectedResultHtml { get; set; }
        public string Hash { get; set; }
        public bool Deleted { get; set; }
        public bool Editable { get; set; }
        public bool Automated { get; set; }
        public bool External { get; set; }
        public Item TestLayer { get; set; }
        public List<Tag> Tags { get; set; }
        public List<Link> Links { get; set; }
        public Status Status { get; set; }
        public Workflow Workflow { get; set; }
    }
    
}