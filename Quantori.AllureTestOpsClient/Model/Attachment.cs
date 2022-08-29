namespace Quantori.AllureTestOpsClient.Model
{
    public class Attachment
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int ContentLength { get; set; }
        public string ContentType { get; set; }
        public string Entity { get; set; }
        public bool Missed { get; set; }
    }
}