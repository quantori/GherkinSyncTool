using System.IO;
using System.Text.RegularExpressions;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using HtmlAgilityPack;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Utils
{
    internal static class FeatureFileUtils
    {
        private static readonly GherkinSyncToolConfig GherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();

        /// <summary>
        /// Takes full path from test case description and writes it to feature file.
        /// </summary>
        /// <param name="workItem"></param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        internal static void InsertTagIdToTheFeatureFile(WorkItem workItem)
        {
            var baseDirectory = new DirectoryInfo(GherkinSyncToolConfig.BaseDirectory);
            if (baseDirectory.Parent is null) throw new DirectoryNotFoundException($"Base directory {baseDirectory} does not have a parent");

            var description = (string) workItem.Fields[WorkItemFields.Description];

            var descriptionHtml = new HtmlDocument();
            descriptionHtml.LoadHtml(description);
            var featureFilePathRaw = descriptionHtml.GetElementbyId(HtmlTagIds.FeatureFilePathId).InnerText;

            var relativePathPattern = $@"{Regex.Escape(baseDirectory.Name)}.*?\.feature";
            var relativePathMatch = Regex.Match(featureFilePathRaw, relativePathPattern, RegexOptions.IgnoreCase);
            var relativePath = relativePathMatch.Value;

            var fullPath = baseDirectory.Parent.FullName + Path.DirectorySeparatorChar + relativePath;

            if (!File.Exists(fullPath)) throw new FileNotFoundException($"The file {fullPath} does not exist");

            var title = (string) workItem.Fields[WorkItemFields.Title];
            var scenarioRegex = new Regex($"Scenario.*:.*{Regex.Escape(title)}", RegexOptions.IgnoreCase);

            var formattedTagId = GherkinHelper.FormatTagId(workItem.Id.ToString());

            TextFilesEditMethods.InsertLineToTheFileRegex(fullPath, scenarioRegex, formattedTagId);
        }
    }
}