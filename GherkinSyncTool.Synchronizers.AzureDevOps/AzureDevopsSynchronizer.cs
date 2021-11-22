using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AzureDevOps.Client;
using GherkinSyncTool.Synchronizers.AzureDevOps.Content;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using GherkinSyncTool.Synchronizers.AzureDevOps.Utils;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using NLog;

namespace GherkinSyncTool.Synchronizers.AzureDevOps
{
    public class AzureDevopsSynchronizer : ISynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

        private readonly AzureDevopsClient _azureDevopsClient;
        private readonly CaseContentBuilder _caseContentBuilder;

        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig =
            ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();

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
            var fullTestCasesIdList = _azureDevopsClient.GetAllTestCasesIds().ToList();
            var testCasesToUpdateFromTheFeatureFiles = new Dictionary<int, WitBatchRequest>();

            var witBatchRequests = new List<WitBatchRequest>();

            var patchDocumentId = -1;

            foreach (var featureFile in featureFiles)
            {
                foreach (var scenario in featureFile.Document.Feature.Children.OfType<Scenario>())
                {
                    var tagIds = scenario.Tags.Where(tag => tag.Name.Contains(_gherkinSyncToolConfig.TagIdPrefix)).ToList();

                    if (tagIds.Count > 1)
                    {
                        Log.Warn($"There are multiple tag IDs in the scenario. Using the last one ID. Scenario name: {scenario.Name}");
                    }

                    var tagId = tagIds.LastOrDefault();

                    // Create test case for feature file that first time sync, no tag id present.  
                    if (tagId is null)
                    {
                        var testCasePatchDocument = _caseContentBuilder.BuildTestCaseDocument(scenario, featureFile, patchDocumentId--);
                        var testCaseBatchRequest = _azureDevopsClient.BuildCreateTestCaseBatchRequest(testCasePatchDocument);
                        witBatchRequests.Add(testCaseBatchRequest);
                    }

                    // Update scenarios that have tag id
                    if (tagId is not null)
                    {
                        var caseId = (int)GherkinHelper.GetTagIdUlong(tagId);
                        if (fullTestCasesIdList.Contains(caseId))
                        {
                            var testCasePatchDocument = _caseContentBuilder.BuildTestCaseDocument(scenario, featureFile);
                            var testCaseBatchRequest = _azureDevopsClient.BuildUpdateTestCaseBatchRequest(caseId, testCasePatchDocument);

                            testCasesToUpdateFromTheFeatureFiles.Add(caseId, testCaseBatchRequest);
                        }
                        else
                        {
                            Log.Warn($"Test case with id {caseId} not found. Missing case will be recreated");
                            var testCasePatchDocument = _caseContentBuilder.BuildTestCaseDocument(scenario, featureFile, patchDocumentId--);
                            var testCaseBatchRequest = _azureDevopsClient.BuildCreateTestCaseBatchRequest(testCasePatchDocument);

                            var tagIdRegexPattern = $@"\s*{GherkinHelper.FormatTagId(caseId.ToString()).Trim()}\s*";
                            
                            TextFilesEditMethods.ReplaceTextInTheFileRegex(featureFile.AbsolutePath, tagIdRegexPattern,"");
                            
                            witBatchRequests.Add(testCaseBatchRequest);
                        }
                    }
                }
            }

            if (testCasesToUpdateFromTheFeatureFiles.Any())
            {
                //Compare test cases from feature files and azure DevOps to add changed ones for the update.
                var casesToUpdateFromTheAzure = _azureDevopsClient.GetWorkItemsBatch(testCasesToUpdateFromTheFeatureFiles.Keys);

                foreach (var (id, witBatchRequest) in testCasesToUpdateFromTheFeatureFiles)
                {
                    try
                    {
                        var witBatchRequestBody = JsonConvert.DeserializeObject<List<WorkItemBatchRequestBody>>(witBatchRequest.Body);
                        if (witBatchRequestBody is null) throw new NullReferenceException();
                        
                        var fieldsToUpdateFeatureFile = new Dictionary<string, string>();
                        
                        foreach (var item in witBatchRequestBody)
                        {
                            fieldsToUpdateFeatureFile.Add(item.Path.Replace("/fields/", ""), item.Value);
                        }

                        var fieldsToUpdateAzure = casesToUpdateFromTheAzure.First(item => item.Id == id).Fields;

                        if (IsDictionariesSimilar(fieldsToUpdateFeatureFile, fieldsToUpdateAzure.ToDictionary(k => k.Key, k => k.Value.ToString())))
                        {
                            testCasesToUpdateFromTheFeatureFiles.Remove(id);
                            Log.Info($"Up-to-date: [{id}] {fieldsToUpdateFeatureFile[$"{WorkItemFields.Title}"]}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Something went wrong with the test case update");
                        _context.IsRunSuccessful = false;
                    }

                }
            }

            witBatchRequests.AddRange(testCasesToUpdateFromTheFeatureFiles.Values);

            if (witBatchRequests.Any())
            {
                List<WorkItem> workItems;
                try
                {
                    workItems = _azureDevopsClient.ExecuteWorkItemBatch(witBatchRequests);
                }
                catch (Exception e)
                {
                    Log.Error(e, "The test cases have not been synchronized");
                    throw;
                }

                foreach (var workItem in workItems)
                {
                    //Rev = 1 means that a test case has been created
                    if (workItem.Rev == 1)
                    {
                        try
                        {
                            FeatureFileUtils.InsertTagIdToTheFeatureFile(workItem);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, $"Something went wrong with the writing ID to the feature file. Test case id: {workItem.Id}");
                            _context.IsRunSuccessful = false;
                        }
                    }
                }
            }

            Log.Debug(@$"Synchronization with AzureDevops finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
        }

        private static bool IsDictionariesSimilar(Dictionary<string, string> dictionaryA, IDictionary<string, string> dictionaryB)
        {
            var isSimilar = true;
            foreach (var fieldToUpdateFeatureFile in dictionaryA)
            {
                var fieldToUpdateFeatureFileNormalized = Regex.Replace(fieldToUpdateFeatureFile.Value, @"\s", "");
                var fieldsToUpdateAzureNormalized = Regex.Replace(dictionaryB[fieldToUpdateFeatureFile.Key], @"\s", "");

                if (!string.Equals(fieldToUpdateFeatureFileNormalized, fieldsToUpdateAzureNormalized,
                    StringComparison.OrdinalIgnoreCase))
                {
                    isSimilar = false;
                    break;
                }
            }

            return isSimilar;
        }
    }
}