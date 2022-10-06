namespace Quantori.AllureTestOpsClient.Model
{
    public class TestCaseContent
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; }
        public Item TestLayer { get; set; }
        public bool Automated { get; set; }
    }
}