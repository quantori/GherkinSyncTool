using System.Collections.Generic;
using System.Linq;
using Gherkin.Ast;
using GherkinSyncTool.Models.Configuration;

namespace GherkinSyncTool.Models.Utils
{
    public static class GherkinFileHelper
    {
        private static readonly GherkinSyncToolConfig GherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();

        public static List<Tag> GetAllTags(Scenario scenario, IFeatureFile featureFile)
        {
            var allTags = new List<Tag>();

            var featureTags = featureFile.Document.Feature.Tags.ToList();
            allTags.AddRange(featureTags);

            var scenarioTags = scenario.Tags.ToList();
            allTags.AddRange(scenarioTags);

            if (scenario.Examples != null && scenario.Examples.Any())
            {
                foreach (var example in scenario.Examples)
                {
                    if (example.Tags != null && example.Tags.Any())
                    {
                        allTags.AddRange(example.Tags);
                    }
                }
            }

            //Remove test case id to not duplicate because it is visible in UI
            allTags.RemoveAll(tag => tag.Name.Contains(GherkinSyncToolConfig.TagIdPrefix));

            return allTags;
        }
    }
}