namespace GherkinSyncTool.Synchronizers.AzureDevOps.Model
{
    /// <summary>
    /// Azure DevOps work item request body
    /// </summary>
    public class WitBatchRequestBody
    {
        public string Path { get; set; }
        public string Value { get; set; }
    }
}