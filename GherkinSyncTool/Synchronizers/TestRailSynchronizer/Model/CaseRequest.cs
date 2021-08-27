using Newtonsoft.Json.Linq;

namespace GherkinSyncTool.Synchronizers.TestRailSynchronizer.Model
{
    public class CaseRequest
    {
        public ulong SectionId { get; init; }
        public string Title { get; init; }
        public CaseCustomFields CustomFields { get; init; }
        public ulong? TemplateId { get; init; }

        public JObject JObjectCustomFields => CustomFields is not null ? JObject.FromObject(CustomFields) : null;
    }
}