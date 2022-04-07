using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.TestRail.Client;
using GherkinSyncTool.Synchronizers.TestRail.Content;
using GherkinSyncTool.Synchronizers.TestRail.Exceptions;
using GherkinSyncTool.Synchronizers.TestRail.Utils;
using NLog;
using TestRail.Types;

namespace GherkinSyncTool.Synchronizers.TestRail
{
    public class TestRailSynchronizer : ISynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly TestRailClientWrapper _testRailClientWrapper;
        private readonly CaseContentBuilder _caseContentBuilder;
        private readonly SectionSynchronizer _sectionSynchronizer;
        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();
        private readonly Context _context;
        private readonly TestRailCaseFields _testRailCaseFields;


        public TestRailSynchronizer(TestRailClientWrapper testRailClientWrapper, CaseContentBuilder caseContentBuilder,
            SectionSynchronizer sectionSynchronizer, Context context, TestRailCaseFields testRailCaseFields)
        {
            _testRailClientWrapper = testRailClientWrapper;
            _caseContentBuilder = caseContentBuilder;
            _sectionSynchronizer = sectionSynchronizer;
            _context = context;
            _testRailCaseFields = testRailCaseFields;
        }

        public void Sync(List<IFeatureFile> featureFiles)
        {
            Log.Info($"# Start synchronization with TestRail");
            _testRailCaseFields.CheckCustomFields();
            var stopwatch = Stopwatch.StartNew();
            var casesToMove = new Dictionary<ulong, List<ulong>>();
            var testRailCases = _testRailClientWrapper.GetCases();
            var featureFilesTagIds = new List<ulong>(); 

            foreach (var featureFile in featureFiles)
            {
                var insertedTagIds = 0;
                var featureFileSectionId = _sectionSynchronizer.GetOrCreateSectionIdFromPath(featureFile.RelativePath);
                foreach (var scenario in featureFile.Document.Feature.Children.OfType<Scenario>())
                {
                    var tagId = scenario.Tags.FirstOrDefault(tag => tag.Name.Contains(_gherkinSyncToolConfig.TagIdPrefix));

                    var caseRequest = _caseContentBuilder.BuildCaseRequest(scenario, featureFile, featureFileSectionId);

                    // Create test case for feature file which is getting synced for the first time, so no tag id present.  
                    if (tagId is null)
                    {
                        Case addCaseResponse;
                        try
                        {
                            addCaseResponse = _testRailClientWrapper.AddCase(caseRequest);
                        }
                        catch (TestRailException e)
                        {
                            Log.Error(e, $"The case has not been created: {scenario.Name}");
                            _context.IsRunSuccessful = false;
                            continue;
                        } 
                        
                        var lineNumberToInsert = scenario.Location.Line - 1 + insertedTagIds;
                        var formattedTagId = GherkinHelper.FormatTagId(addCaseResponse.Id.ToString());
                        TextFilesEditMethods.InsertLineToTheFile(featureFile.AbsolutePath, lineNumberToInsert,
                            formattedTagId);
                        insertedTagIds++;
                    }

                    // Update scenarios that have tag id
                    if (tagId is not null)
                    {
                        var caseId = GherkinHelper.GetTagIdUlong(tagId);
                        featureFilesTagIds.Add(caseId);
                            
                        var testRailCase = testRailCases.FirstOrDefault(c => c.Id == caseId);
                        if (testRailCase is null)
                        {
                            Log.Warn($"Case with id {caseId} not found. Recreating missing case");
                            try
                            {
                                testRailCase = _testRailClientWrapper.AddCase(caseRequest);
                            }
                            catch (TestRailException e)
                            {
                                Log.Error(e, $"The case has not been created: {scenario.Name}");
                                _context.IsRunSuccessful = false;
                                continue;
                            }
                            var formattedTagId = GherkinHelper.FormatTagId(testRailCase.Id.ToString());
                            TextFilesEditMethods.ReplaceLineInTheFile(featureFile.AbsolutePath,
                                tagId.Location.Line - 1 + insertedTagIds, formattedTagId);
                        }
                        else
                        {
                            try
                            {
                                _testRailClientWrapper.UpdateCase(testRailCase, caseRequest);    
                            }
                            catch (TestRailException e)
                            {
                                Log.Error(e, $"The case has not been updated: {scenario.Name}");
                                _context.IsRunSuccessful = false;
                            }
                        }
                        
                        var testRailSectionId = testRailCase.SectionId;
                        AddCasesToMove(testRailSectionId, featureFileSectionId, caseId, casesToMove);
                    }
                }
            }

            MoveCasesToNewSections(casesToMove);

            DeleteNotExistingScenarios(testRailCases, featureFilesTagIds);

            _sectionSynchronizer.MoveNotExistingSectionsToArchive();

            Log.Debug(@$"Synchronization with TestRail finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
        }

        private void DeleteNotExistingScenarios(IList<Case> testRailCases, List<ulong> featureFilesTagIds)
        {
            var testRailTagIds = testRailCases.Where(c => c.Id is not null).Select(c => c.Id.Value);
            var tagsToDelete = testRailTagIds.Except(featureFilesTagIds).ToList();
            if (!tagsToDelete.Any())
            {
                return;
            }
            
            try
            {
                _testRailClientWrapper.DeleteCases(tagsToDelete);
            }
            catch (TestRailException e)
            {
                Log.Error(e, $"The cases has not been deleted: {string.Join(", ", tagsToDelete)}");
                _context.IsRunSuccessful = false;
            }
        }

        private void MoveCasesToNewSections(Dictionary<ulong, List<ulong>> casesToMove)
        {
            foreach (var (key, value) in casesToMove)
            {
                try
                {
                    _testRailClientWrapper.MoveCases(key, value);
                }
                catch (TestRailException e)
                {
                    Log.Error(e, $"The case has not been moved: {value}");
                    _context.IsRunSuccessful = false;
                }
            }
        }

        /// <summary>
        /// Adds cases to move to new section, if changed
        /// </summary>
        /// <param name="oldSectionId">id of old section</param>
        /// <param name="currentSectionId">id of current section</param>
        /// <param name="caseId">case id</param>
        /// <param name="casesToMove">IDictionary where key is section id and the value is case ids collection</param>
        private void AddCasesToMove(ulong? oldSectionId, ulong? currentSectionId, ulong caseId,
            IDictionary<ulong, List<ulong>> casesToMove)
        {
            if (oldSectionId.HasValue && currentSectionId.HasValue &&
                !oldSectionId.Equals(currentSectionId))
            {
                var key = currentSectionId.Value;
                if (!casesToMove.ContainsKey(key))
                    casesToMove.Add(key, new List<ulong>() {caseId});
                else casesToMove[key].Add(caseId);
            }
        }
    }
}