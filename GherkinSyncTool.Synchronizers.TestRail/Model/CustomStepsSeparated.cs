using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class CustomStepsSeparated
    {
        [JsonProperty("content")] 
        public string Content { get; set; }
    }
}