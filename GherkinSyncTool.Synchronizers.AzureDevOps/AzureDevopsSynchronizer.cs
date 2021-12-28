using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
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

        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig =
            ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();

        private readonly Context _context;

        public AzureDevopsSynchronizer(AzureDevopsClient azureDevopsClient, CaseContentBuilder caseContentBuilder,
            Context context)
        {
            _azureDevopsClient = azureDevopsClient;
            _caseContentBuilder = caseContentBuilder;
            _context = context;
        }

        public void Sync(List<IFeatureFile> featureFiles)
        {
            Log.Info($"# Start synchronization with AzureDevops");
            var stopwatch = Stopwatch.StartNew();
            var testCasesIdFromAzureDevops = _azureDevopsClient.GetAllTestCasesIds().ToList();
            var testCasesFromTheFeatureFiles = new Dictionary<int, WitBatchRequest>();

            var witBatchRequests = new List<WitBatchRequest>();

            var patchDocumentId = -1;

            foreach (var featureFile in featureFiles)
            {
                if (featureFile.Document.Feature is null)
                {
                    Log.Warn($"Feature file is empty: {featureFile.RelativePath}");
                    continue;
                }
                
                foreach (var scenario in featureFile.Document.Feature.Children.OfType<Scenario>())
                {
                    var tagIds = scenario.Tags.Where(tag => tag.Name.Contains(_gherkinSyncToolConfig.TagIdPrefix))
                        .ToList();

                    if (tagIds.Count > 1)
                    {
                        Log.Warn($"There are multiple tag IDs in the scenario. Using the last one ID. Scenario name: {scenario.Name}");
                    }

                    var tagId = tagIds.LastOrDefault();

                    // Create test case for feature file that first time sync, no tag id present.  
                    if (tagId is null)
                    {
                        WitBatchRequest testCaseBatchRequest;
                        try
                        {
                            testCaseBatchRequest = _caseContentBuilder.BuildTestCaseBatchRequest(scenario, featureFile, patchDocumentId--);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e, "Something went wrong with the test case request building.");
                            _context.IsRunSuccessful = false;
                            continue;
                        }

                        witBatchRequests.Add(testCaseBatchRequest);
                    }

                    // Update scenarios that have tag id
                    if (tagId is not null)
                    {
                        var caseId = (int)GherkinHelper.GetTagIdUlong(tagId);
                        if (testCasesIdFromAzureDevops.Contains(caseId))
                        {
                            try
                            {
                                testCasesFromTheFeatureFiles.Add(caseId, null);
                            }
                            catch (ArgumentException)
                            {
                                Log.Error( $"A scenario with the same ID already exists. ID: {caseId}");
                                _context.IsRunSuccessful = false;
                                continue;
                            }
                            
                            WitBatchRequest testCaseBatchRequest;
                            try
                            {
                                testCaseBatchRequest = _caseContentBuilder.BuildUpdateTestCaseBatchRequest(scenario, featureFile, caseId);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, "Something went wrong with the test case request building.");
                                _context.IsRunSuccessful = false;
                                continue;
                            }

                            testCasesFromTheFeatureFiles[caseId] = testCaseBatchRequest;
                        }
                        else
                        {
                            Log.Warn($"Test case with id {caseId} not found. Missing case will be recreated");

                            WitBatchRequest testCaseBatchRequest;
                            try
                            {
                                testCaseBatchRequest = _caseContentBuilder.BuildTestCaseBatchRequest(scenario, featureFile, patchDocumentId--);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e, "Something went wrong with the test case request building.");
                                _context.IsRunSuccessful = false;
                                continue;
                            }

                            var tagIdRegexPattern = $@"\s*{GherkinHelper.FormatTagId(caseId.ToString()).Trim()}\s*";

                            TextFilesEditMethods.ReplaceTextInTheFileRegex(featureFile.AbsolutePath, tagIdRegexPattern,
                                "");

                            witBatchRequests.Add(testCaseBatchRequest);
                        }
                    }
                }
            }

            if (testCasesFromTheFeatureFiles.Any())
            {
                var testCasesToUpdate = GetTestCasesToUpdate(testCasesFromTheFeatureFiles);
                witBatchRequests.AddRange(testCasesToUpdate);
            }
            
            //Close deleted scenarios
            var testCasesToClose = GetTestCasesToClose(testCasesFromTheFeatureFiles.Keys.ToList());
            witBatchRequests.AddRange(testCasesToClose);

            if (witBatchRequests.Any())
            {
                SynchronizeTestCases(witBatchRequests);
            }

            Log.Debug(@$"Synchronization with AzureDevops finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
        }

        private IEnumerable<WitBatchRequest> GetTestCasesToClose(IEnumerable<int> testCasesFromTheFeatureFiles)
        {
            var testCaseIdsFromAzureDevops = _azureDevopsClient.GetSyncedTestCasesIds().ToList();
            var result = new List<WitBatchRequest>();
            var testCasesToClose = testCaseIdsFromAzureDevops.Except(testCasesFromTheFeatureFiles).ToList();
            if (!testCasesToClose.Any())
            {
                return result;
            }

            foreach (var id in testCasesToClose)
            {
                var updateStateBatchRequest = _caseContentBuilder.BuildUpdateStateBatchRequest(id, TestCaseState.Closed);
                result.Add(updateStateBatchRequest);
            }

            return result;
        }

        private void SynchronizeTestCases(List<WitBatchRequest> witBatchRequests)
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

        private IEnumerable<WitBatchRequest> GetTestCasesToUpdate(Dictionary<int, WitBatchRequest> testCasesFromTheFeatureFiles)
        {
            var result = new List<WitBatchRequest>();
            //Compare test cases from feature files and azure DevOps to update only changed ones.
            var testCasesToUpdateFromTheAzure = _azureDevopsClient.GetWorkItemsBatch(testCasesFromTheFeatureFiles.Keys);

            foreach (var (id, witBatchRequest) in testCasesFromTheFeatureFiles)
            {
                try
                {
                    var fieldsToUpdateFeatureFile = witBatchRequest.GetFields();
                    var fieldsToUpdateAzure = testCasesToUpdateFromTheAzure.First(item => item.Id == id).Fields;

                    if (IsTestCaseFieldsSimilar(fieldsToUpdateFeatureFile,
                        fieldsToUpdateAzure.ToDictionary(k => k.Key, k => k.Value.ToString())))
                    {
                        Log.Info($"Up-to-date: [{id}] {fieldsToUpdateFeatureFile[$"{WorkItemFields.Title}"]}");
                        continue;
                    }

                    result.Add(witBatchRequest);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Something went wrong with the test case update");
                    _context.IsRunSuccessful = false;
                }
            }

            return result;
        }

        private static bool IsTestCaseFieldsSimilar(Dictionary<string, string> dictionaryA,
            IDictionary<string, string> dictionaryB)
        {
            foreach (var (fieldKey, fieldValue) in dictionaryA)
            {
                if (!dictionaryB.ContainsKey(fieldKey))
                {
                    //Continue if no parameters field exists and the value is null.
                    if ((fieldKey.Equals(WorkItemFields.Parameters) || fieldKey.Equals(WorkItemFields.LocalDataSource)) && fieldValue is null)
                    {
                        continue;
                    }

                    return false;
                }

                var fieldANormalized = fieldValue.RemoveWhitespaceCharacters().ToLowerInvariant();
                var fieldBNormalized = dictionaryB[fieldKey].RemoveWhitespaceCharacters().ToLowerInvariant();

                //Compare Tags
                if (string.Equals(fieldKey, WorkItemFields.Tags))
                {
                    const char separator = ';';
                    var tagsA = fieldANormalized.Split(separator).ToList();
                    tagsA.Sort();
                    var tagsB = fieldBNormalized.Split(separator).ToList();
                    tagsB.Sort();

                    if (!tagsA.SequenceEqual(tagsB))
                    {
                        return false;
                    }

                    continue;
                }

                if (!string.Equals(fieldANormalized, fieldBNormalized, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }
}