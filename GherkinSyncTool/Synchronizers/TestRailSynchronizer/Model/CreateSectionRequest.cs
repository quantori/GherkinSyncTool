namespace GherkinSyncTool.Synchronizers.TestRailSynchronizer.Model
{
    public class CreateSectionRequest
    {
        public ulong ProjectId { get; init; }
        public string Description { get; init; }
        public ulong? ParentId { get; init; }
        public ulong SuiteId { get; init; }
        public string Name { get; init; }
        public CreateSectionRequest(ulong projectId, ulong? parentId, ulong suiteId, string name, string description)
        {
            ProjectId = projectId;
            ParentId = parentId;
            SuiteId = suiteId;
            Name = name;
            Description = description;
        }
    }
}