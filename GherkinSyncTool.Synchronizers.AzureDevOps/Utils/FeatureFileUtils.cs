using System.IO;
using System.Text.RegularExpressions;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Utils
{
    internal static class FeatureFileUtils
    {
        private static readonly GherkinSyncToolConfig GherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();
       
        /// <summary>
        /// File path takes form test case description and writes to the scenario
        /// </summary>
        /// <param name="workItem"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        internal static void InsertTagIdToTheFeatureFile(WorkItem workItem)
        {
            var baseDirectory = new DirectoryInfo(GherkinSyncToolConfig.BaseDirectory);
            if (baseDirectory.Parent is null) throw new DirectoryNotFoundException($"Base directory {baseDirectory} does not have a parent");

            var description = (string)workItem.Fields[WorkItemFields.Description];
            var relativePathPattern = $@"{Regex.Escape(baseDirectory.Name)}.*\.feature";
            var relativePathRegex = new Regex(relativePathPattern, RegexOptions.IgnoreCase);
            var relativePathMatch = relativePathRegex.Match(description);
            var relativePath = relativePathMatch.Value;
            var fullPath = baseDirectory.Parent.FullName + Path.DirectorySeparatorChar + relativePath;

            var title = (string)workItem.Fields[WorkItemFields.Title];
            var scenarioRegex = new Regex($"Scenario.*:.*{Regex.Escape(title)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var formattedTagId = GherkinHelper.FormatTagId(workItem.Id.ToString());
            TextFilesEditMethods.InsertLineToTheFileRegex(fullPath, scenarioRegex, formattedTagId);
        }
    }
}