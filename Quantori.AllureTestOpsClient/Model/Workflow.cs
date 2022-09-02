using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class Workflow : Item
    {
        public List<Status> Statuses { get; set; }
    }
}