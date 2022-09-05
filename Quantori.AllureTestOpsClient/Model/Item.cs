namespace Quantori.AllureTestOpsClient.Model
{
    public class Item
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public ulong CreatedDate { get; set; }
        public ulong LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }
}