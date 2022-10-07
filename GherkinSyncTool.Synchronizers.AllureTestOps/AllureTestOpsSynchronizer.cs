using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AllureTestOps.Client;
using GherkinSyncTool.Synchronizers.AllureTestOps.Content;
using GherkinSyncTool.Synchronizers.AllureTestOps.Exception;
using GherkinSyncTool.Synchronizers.AllureTestOps.Model;
using NLog;
using Quantori.AllureTestOpsClient.Model;
using Scenario = Gherkin.Ast.Scenario;

namespace GherkinSyncTool.Synchronizers.AllureTestOps
{
    public class AllureTestOpsSynchronizer : ISynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();
        private readonly AllureClientWrapper _allureClientWrapper;
        private readonly Context _context;
        private readonly CaseContentBuilder _caseContentBuilder;
        private readonly AllureTestOpsSettings _allureTestOpsSettings =
            ConfigurationManager.GetConfiguration<AllureTestOpsConfigs>().AllureTestOpsSettings;

        public AllureTestOpsSynchronizer(AllureClientWrapper allureClientWrapper, Context context, CaseContentBuilder caseContentBuilder)
        {
            _allureClientWrapper = allureClientWrapper;
            _context = context;
            _caseContentBuilder = caseContentBuilder;
        }

        public void Sync(List<IFeatureFile> featureFiles)
        {
            var stopwatch = Stopwatch.StartNew();
            Log.Info("# Start synchronization with Allure TestOps");

            var allureTestCases = _allureClientWrapper.GetAllTestCases().ToList();
            var featureFilesTagIds = new List<long>();

            foreach (var featureFile in featureFiles)
            {
                var insertedTagIdsCount = 0;

                foreach (var scenario in featureFile.Document.Feature.Children.OfType<Scenario>())
                {
                    var tagId = scenario.Tags.FirstOrDefault(tag => tag.Name.Contains(_gherkinSyncToolConfig.TagIdPrefix));

                    var caseRequestExtended = _caseContentBuilder.BuildCaseRequest(scenario, featureFile);

                    // Create test case for feature file which is getting synced for the first time, so no tag id present.  
                    if (tagId is null)
                    {
                        var newTestCase = CreateNewTestCase(caseRequestExtended);
                        if (newTestCase is null) continue;

                        var lineNumberToInsert = scenario.Location.Line - 1 + insertedTagIdsCount;
                        var formattedTagId = GherkinHelper.FormatTagId(newTestCase.Id.ToString());
                        TextFilesEditMethods.InsertLineToTheFile(featureFile.AbsolutePath, lineNumberToInsert,
                            formattedTagId);
                        insertedTagIdsCount++;
                        featureFilesTagIds.Add(newTestCase.Id);
                    }

                    // Update scenarios that have tag id
                    if (tagId is not null)
                    {
                        var id = GherkinHelper.GetTagId(tagId);
                        featureFilesTagIds.Add(id);
                        
                        var allureTestCase = allureTestCases.FirstOrDefault(c => c.Id == id);
                        if (allureTestCase is null)
                        {
                            Log.Warn($"Case with id {id} not found. Recreating missing case");
                            var newTestCase = CreateNewTestCase(caseRequestExtended);
                            if (newTestCase is null) continue;
                            
                            var formattedTagId = GherkinHelper.FormatTagId(newTestCase.Id.ToString());
                            TextFilesEditMethods.ReplaceLineInTheFile(featureFile.AbsolutePath,
                                tagId.Location.Line - 1 + insertedTagIdsCount, formattedTagId);
                            
                            featureFilesTagIds.Add(newTestCase.Id);
                        }
                        else
                        {
                            try
                            {
                                _allureClientWrapper.UpdateTestCase(allureTestCase, caseRequestExtended);
                            }
                            catch (AllureException e)
                            {
                                Log.Error(e, $"The test case has not been updated: {caseRequestExtended.CreateTestCaseRequest.Name}");
                                _context.IsRunSuccessful = false;
                            }
                        }
                    }
                }
            }
            
            DeleteNotExistingScenarios(featureFilesTagIds);
            Log.Debug(@$"Synchronization with Allure TestOps finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
        }

        private void DeleteNotExistingScenarios(List<long> featureFilesIDs)
        {
            var query = $"tag is \"{TagsConstants.ToolId + _allureTestOpsSettings.GherkinSyncToolId}\"";
            var allureTestCases = _allureClientWrapper.SearchAllTestCasesWithQuery(query).ToList();
            
            var allureIDs = allureTestCases.Select(c => c.Id);
            var idsToDelete = allureIDs.Except(featureFilesIDs).ToList();
            if (!idsToDelete.Any())
            {
                return;
            }
            
            try
            {
                _allureClientWrapper.RemoveTestCases(idsToDelete);
            }
            catch (AllureException e)
            {
                Log.Error(e, $"The cases has not been deleted: {string.Join(", ", idsToDelete)}");
                _context.IsRunSuccessful = false;
            }
        }

        private TestCase CreateNewTestCase(CreateTestCaseRequestExtended caseRequestExtended)
        {
            TestCase newTestCase = null;
            try
            {
                newTestCase = _allureClientWrapper.CreateTestCase(caseRequestExtended.CreateTestCaseRequest);
            }
            catch (AllureException e)
            {
                Log.Error(e, $"The case has not been created: {caseRequestExtended.CreateTestCaseRequest.Name}");
                _context.IsRunSuccessful = false;
            }

            if (newTestCase is null) return null;
            
            if (caseRequestExtended.StepsAttachments.Any())
            {
                try
                {
                    _allureClientWrapper.UploadStepAttachments(caseRequestExtended, newTestCase);
                }
                catch (AllureException e)
                {
                    Log.Error(e, $"The test case steps attachment has not been uploaded: [{newTestCase.Id}]{caseRequestExtended.CreateTestCaseRequest.Name}");
                    _context.IsRunSuccessful = false;
                }
            }

            return newTestCase;
        }
    }
}