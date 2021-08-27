using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.TestRailSynchronizer.Model
{
    public class CustomStepsSeparated
    {
        [JsonProperty("content")] 
        public string Content { get; init; }
    }
}