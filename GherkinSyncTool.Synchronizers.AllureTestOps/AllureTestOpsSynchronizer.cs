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
using NLog;
using Quantori.AllureTestOpsClient.Model;
using Scenario = Gherkin.Ast.Scenario;

namespace GherkinSyncTool.Synchronizers.AllureTestOps
{
    public class AllureTestOpsSynchronizer : ISynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();
        private readonly AllureClient _allureClient;
        private readonly Context _context;
        private readonly CaseContentBuilder _caseContentBuilder;

        public AllureTestOpsSynchronizer(AllureClient allureClient, Context context, CaseContentBuilder caseContentBuilder)
        {
            _allureClient = allureClient;
            _context = context;
            _caseContentBuilder = caseContentBuilder;
        }

        public void Sync(List<IFeatureFile> featureFiles)
        {
            var stopwatch = Stopwatch.StartNew();
            Log.Info("# Start synchronization with Allure TestOps");

            var allureTestCases = _allureClient.GetAllTestCases().ToList();

            foreach (var featureFile in featureFiles)
            {
                var insertedTagIdsCount = 0;

                foreach (var scenario in featureFile.Document.Feature.Children.OfType<Scenario>())
                {
                    var tagId = scenario.Tags.FirstOrDefault(tag => tag.Name.Contains(_gherkinSyncToolConfig.TagIdPrefix));

                    var caseRequest = _caseContentBuilder.BuildCaseRequest(scenario, featureFile);

                    // Create test case for feature file which is getting synced for the first time, so no tag id present.  
                    if (tagId is null)
                    {
                        var newTestCase = CreateTestCase(caseRequest);
                        if(newTestCase is null) continue;
                        
                        var lineNumberToInsert = scenario.Location.Line - 1 + insertedTagIdsCount;
                        var formattedTagId = GherkinHelper.FormatTagId(newTestCase.Id.ToString());
                        TextFilesEditMethods.InsertLineToTheFile(featureFile.AbsolutePath, lineNumberToInsert,
                            formattedTagId);
                        insertedTagIdsCount++;
                    }

                    // Update scenarios that have tag id
                    if (tagId is not null)
                    {
                        var caseIdFromFile = GherkinHelper.GetTagId(tagId);
                        //featureFilesTagIds.Add(caseId);
                        var allureTestCase = allureTestCases.FirstOrDefault(c => c.Id == caseIdFromFile);
                        if (allureTestCase is null)
                        {
                            Log.Warn($"Case with id {caseIdFromFile} not found. Recreating missing case");
                            var newTestCase = CreateTestCase(caseRequest);
                            if(newTestCase is null) continue;
                            
                            var formattedTagId = GherkinHelper.FormatTagId(newTestCase.Id.ToString());
                            TextFilesEditMethods.ReplaceLineInTheFile(featureFile.AbsolutePath,
                                tagId.Location.Line - 1 + insertedTagIdsCount, formattedTagId);
                        }
                        else
                        {
                            try
                            {
                                _allureClient.UpdateTestCase(allureTestCase, caseRequest);    
                            }
                            catch (AllureException e)
                            {
                                Log.Error(e, $"The test case has not been updated: {caseRequest.Name}");
                                _context.IsRunSuccessful = false;
                            }
                        }
                    }
                }
            }

            Log.Debug(@$"Synchronization with Allure TestOps finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
        }

        private TestCase CreateTestCase(CreateTestCaseRequest caseRequest)
        {
            TestCase result = null;
            try
            {
                result = _allureClient.AddTestCase(caseRequest);
                return result;
            }
            catch (AllureException e)
            {
                Log.Error(e, $"The case has not been created: {caseRequest.Name}");
                _context.IsRunSuccessful = false;
                return result;
            }
        }
    }
}