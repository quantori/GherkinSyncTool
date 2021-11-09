using System.Collections.Generic;
using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class CustomFieldsModel
    {
        [JsonProperty("configs")] 
        public Config[] Configs { get; set; }
    }

    public class Config
    {
        [JsonProperty("context")]
        public CustomFieldContext Context { get; set; }
    }

    public class CustomFieldContext
    {
        [JsonProperty("is_global")]
        public bool IsGlobal { get; set; }

        [JsonProperty("project_ids")]
        public List<ulong> ProjectIds { get; set; }
    }
}