using System.Collections.Generic;
using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.TestRailSynchronizer.Model
{
    public class CaseCustomFields
    {
        [JsonProperty("custom_preconds")] 
        public string CustomPreconditions { get; init; }

        [JsonProperty("custom_steps_separated")]
        public List<CustomStepsSeparated> CustomStepsSeparated { get; init; }

        [JsonProperty("custom_tags")] 
        public string CustomTags { get; init; }
    }
}