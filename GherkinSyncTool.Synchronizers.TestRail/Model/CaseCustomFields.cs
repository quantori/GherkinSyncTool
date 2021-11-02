using System.Collections.Generic;
using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class CaseCustomFields
    {
        [JsonProperty("custom_preconds")] 
        public string Preconditions { get; init; }

        [JsonProperty("custom_steps_separated")]
        public List<CustomStepsSeparated> StepsSeparated { get; init; }

        [JsonProperty("custom_tags")] 
        public string Tags { get; init; }
        
        [JsonProperty("custom_gherkinsynctool_id")] 
        public string GherkinSyncToolId { get; init; }
    }
}