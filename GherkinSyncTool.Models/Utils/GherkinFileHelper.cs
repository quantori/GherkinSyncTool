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
            List<Tag> allTags = new List<Tag>();

            var featureTags = featureFile.Document.Feature.Tags.ToList();
            if (featureTags.Any())
            {
                allTags.AddRange(featureTags);
            }

            var scenarioTags = scenario.Tags.ToList();
            if (scenarioTags.Any())
            {
                allTags.AddRange(scenarioTags);
            }

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

            allTags.RemoveAll(tag => tag.Name.Contains(GherkinSyncToolConfig.TagIdPrefix));

            return allTags;
        }
    }
}