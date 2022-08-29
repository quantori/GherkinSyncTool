using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class Workflow
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public List<Status> Statuses { get; set; }
        public ulong CreatedDate { get; set; }
        public ulong LastModifiedDate { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
    }
}