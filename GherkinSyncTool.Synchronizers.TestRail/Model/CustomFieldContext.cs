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
        
        [JsonProperty("options")]
        public CustomFieldOptions Options { get; set; }
    }
    
    public class CustomFieldOptions
    {
        [JsonProperty("is_required")]
        public bool IsRequired { get; set; }
        
        [JsonProperty("default_value")]
        public string DefaultValue { get; set; }
        
        [JsonProperty("items")]
        public string Items { get; set; }
    }

    public class CustomFieldContext
    {
        [JsonProperty("is_global")]
        public bool IsGlobal { get; set; }

        [JsonProperty("project_ids")]
        public List<ulong> ProjectIds { get; set; }
    }
}