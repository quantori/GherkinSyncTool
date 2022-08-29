using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class TestCase
    {
        public ulong Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
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
        public TestLayer TestLayer { get; set; }
        public List<Tag> Tags { get; set; }
        public List<Link> Links { get; set; }
        public Status Status { get; set; }
        public Workflow Workflow { get; set; }
        public ulong CreatedDate { get; set; }
        public ulong LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }
    
}