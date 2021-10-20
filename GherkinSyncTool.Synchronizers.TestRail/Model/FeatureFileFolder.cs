using System.Collections.Generic;

namespace GherkinSyncTool.Synchronizers.TestRail.Model
{
    public class FeatureFileFolder
    {
        public string Name { get; set; }
        public List<FeatureFileFolder> ChildFolders { get; set; } = new();
    }
}