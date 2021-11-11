using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.AzureDevOps.Client;
using GherkinSyncTool.Synchronizers.AzureDevOps.Content;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using GherkinSyncTool.Synchronizers.AzureDevOps.Utils;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NLog;

namespace GherkinSyncTool.Synchronizers.AzureDevOps
{
    public class AzureDevopsSynchronizer : ISynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

        private readonly AzureDevopsClient _azureDevopsClient;
        private readonly CaseContentBuilder _caseContentBuilder;
        
        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();
        private readonly Context _context;

        public AzureDevopsSynchronizer(AzureDevopsClient azureDevopsClient, CaseContentBuilder caseContentBuilder, Context context)
        {
            _azureDevopsClient = azureDevopsClient;
            _caseContentBuilder = caseContentBuilder;
            _context = context;
        }

        public void Sync(List<IFeatureFile> featureFiles)
        {
            Log.Info($"# Start synchronization with AzureDevops");
            var stopwatch = Stopwatch.StartNew();
            
            var caseToCreate = new List<WitBatchRequest>();
            int caseToCreateId = -1;

            foreach (var featureFile in featureFiles)
            {
                foreach (var scenario in featureFile.Document.Feature.Children.OfType<Scenario>())
                {
                    var tagId = scenario.Tags.FirstOrDefault(tag => tag.Name.Contains(_gherkinSyncToolConfig.TagIdPrefix));
                    
                    // Create test case for feature file that first time sync, no tag id present.  
                    if (tagId is null)
                    {
                        var testCasePatchDocument = _caseContentBuilder.BuildTestCaseDocument(scenario, featureFile, caseToCreateId--);
                        var testCaseBatchRequest = _azureDevopsClient.CreateTestCaseBatchRequest(testCasePatchDocument);
                        caseToCreate.Add(testCaseBatchRequest);
                    }
                }
            }
            
            try
            {
                if (caseToCreate.Any())
                {
                    var workItems = _azureDevopsClient.ExecuteWorkItemBatch(caseToCreate);
                
                    foreach (var workItem in workItems)
                    {
                        InsertTagIdToTheFeatureFile(workItem);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, $"The cases has not been created");
                _context.IsRunSuccessful = false;
            }

            Log.Debug(@$"Synchronization with AzureDevops finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
        }

        private void InsertTagIdToTheFeatureFile(WorkItem workItem)
        {
            var baseDirectory = new DirectoryInfo(_gherkinSyncToolConfig.BaseDirectory);
            if (baseDirectory.Parent is null) throw new DirectoryNotFoundException($"Base directory {baseDirectory} does not have a parent");

            var description = (string)workItem.Fields[WorkItemFields.Description];
            var relativePathPattern = $@"{Regex.Escape(baseDirectory.Name)}.*\.feature";
            var relativePathRegex = new Regex(relativePathPattern, RegexOptions.IgnoreCase);
            var relativePathMatch = relativePathRegex.Match(description);
            var relativePath = relativePathMatch.Value;
            var fullPath = baseDirectory.Parent.FullName + Path.DirectorySeparatorChar + relativePath;

            var title = (string)workItem.Fields[WorkItemFields.Title];
            var scenarioRegex = new Regex($"Scenario.*:.*{Regex.Escape(title)}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            var formattedTagId = _gherkinSyncToolConfig.FormattingSettings.TagIndentation + _gherkinSyncToolConfig.TagIdPrefix + workItem.Id;
            TextFilesEditMethods.InsertLineToTheFile(fullPath, scenarioRegex, formattedTagId);
        }
    }
}