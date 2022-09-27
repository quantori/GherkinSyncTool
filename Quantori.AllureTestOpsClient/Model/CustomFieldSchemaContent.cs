namespace Quantori.AllureTestOpsClient.Model
{
    public class CustomFieldSchemaContent
    {
        public long Id { get; set; }
        public int ProjectId { get; set; }
        public string Key { get; set; }
        public ulong CreatedDate { get; set; }
        public ulong LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
        public Item CustomField { get; set; }
    }
}