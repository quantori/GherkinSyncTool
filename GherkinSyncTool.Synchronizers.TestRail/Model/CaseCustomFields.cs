using System.Collections.Generic;
using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class CaseCustomFields
    {
        [JsonProperty("custom_preconds")] 
        public string Preconditions { get; set; }

        [JsonProperty("custom_steps_separated")]
        public List<CustomStepsSeparated> StepsSeparated { get; set; }

        [JsonProperty("custom_tags")] 
        public string Tags { get; set; }
        
        [JsonProperty("custom_gherkinsynctool_id")] 
        public string GherkinSyncToolId { get; set; }
        
        [JsonProperty("custom_automation_type")] 
        public uint? AutomationType { get; set; }
    }
}