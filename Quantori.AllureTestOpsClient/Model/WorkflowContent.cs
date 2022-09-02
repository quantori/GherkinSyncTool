using System.Collections.Generic;

namespace Quantori.AllureTestOpsClient.Model
{
    public class WorkflowContent : Item
    {
        /// <summary>
        /// Workflow ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// Workflow name
        /// </summary>
        public string Name { get; set; }
        public List<Status> Statuses { get; set; }
    }
}