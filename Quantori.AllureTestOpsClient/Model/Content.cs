namespace Quantori.AllureTestOpsClient.Model
{
    public class Content
    {
        public ulong Id { get; set; }
        public string Name { get; set; }
        public Status Status { get; set; }
        public TestLayer TestLayer { get; set; }
        public bool Automated { get; set; }
    }
}