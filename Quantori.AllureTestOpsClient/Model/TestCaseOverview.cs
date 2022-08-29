using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class TestCaseOverview
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
        public string Style { get; set; }
        public Layer Layer { get; set; }
        public Scenario Scenario { get; set; }
        public List<Tag> Tags { get; set; }
        public List<Parameter> Parameters { get; set; }
        public List<Example> Examples { get; set; }
        public List<Link> Links { get; set; }
        public List<Mute> Mutes { get; set; }
        public List<CustomFieldItem> CustomFields { get; set; }
        public List<TestKey> TestKeys { get; set; }
        public List<Issue> Issues { get; set; }
        public List<Member> Members { get; set; }
        public List<Relation> Relations { get; set; }
        public Status Status { get; set; }
        public Workflow Workflow { get; set; }
        public ulong CreatedDate { get; set; }
        public ulong LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }

    public class Example
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public List<Parameter> Parameters { get; set; }
    }

    public class Issue
    {
        public int Id { get; set; }
        public int IntegrationId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
        public string Summary { get; set; }
        public string Status { get; set; }
        public bool Closed { get; set; }
    }

    public class Layer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public ulong CreatedDate { get; set; }
        public ulong LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }

    public class Mute
    {
        public int Id { get; set; }
        public int TestCaseId { get; set; }
        public string Name { get; set; }
        public string Reason { get; set; }
        public string ReasonHtml { get; set; }
        public List<Issue> Issues { get; set; }
        public ulong CreatedDate { get; set; }
        public ulong LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    public class Relation
    {
        public int Id { get; set; }
        public Target Target { get; set; }
        public string Type { get; set; }
    }

    public class Target
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class TestKey
    {
        public int Id { get; set; }
        public int IntegrationId { get; set; }
        public string Name { get; set; }
        public string Url { get; set; }
    }
}