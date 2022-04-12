namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class CreateSectionRequest
    {
        public ulong ProjectId { get; }
        public string Description { get; }
        public ulong? ParentId { get; }
        public ulong SuiteId { get; }
        public string Name { get; }

        public CreateSectionRequest(ulong projectId, ulong? parentId, ulong suiteId, string name,
            string description = null)
        {
            ProjectId = projectId;
            ParentId = parentId;
            SuiteId = suiteId;
            Name = name;
            Description = description;
        }
    }
}