namespace Quantori.AllureTestOpsClient.Model
{
    public class WorkflowSchema : Item
    {
        public int ProjectId { get; set; }
        public Item Workflow { get; set; }
        public string Type { get; set; }
    }
}