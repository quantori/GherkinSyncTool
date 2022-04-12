using Newtonsoft.Json.Linq;

namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class CaseRequest
    {
        public ulong SectionId { get; set; }
        public string Title { get; set; }
        public CaseCustomFields CustomFields { get; set; }
        public ulong? TemplateId { get; set; }
        public string References { get; set; }
        public ulong? PriorityId { get; set; }

        public JObject JObjectCustomFields => CustomFields is not null ? JObject.FromObject(CustomFields) : null;
    }
}