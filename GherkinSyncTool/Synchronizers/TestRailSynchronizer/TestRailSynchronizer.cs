using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Gherkin.Ast;
using GherkinSyncTool.Configuration;
using GherkinSyncTool.Exceptions;
using GherkinSyncTool.Interfaces;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Client;
using GherkinSyncTool.Synchronizers.TestRailSynchronizer.Content;
using GherkinSyncTool.Utils;
using NLog;
using TestRail.Types;

namespace GherkinSyncTool.Synchronizers.TestRailSynchronizer
{
    public class TestRailSynchronizer : ISynchronizer
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly TestRailClientWrapper _testRailClientWrapper;
        private readonly CaseContentBuilder _caseContentBuilder;
        private readonly SectionSynchronizer _sectionSynchronizer;
        private readonly GherkynSyncToolConfig _config = ConfigurationManager.GetConfiguration();
        private readonly string _tagIndentation;
        private readonly Context _context;

        public TestRailSynchronizer(TestRailClientWrapper testRailClientWrapper, CaseContentBuilder caseContentBuilder,
            SectionSynchronizer sectionSynchronizer, Context context)
        {
            _testRailClientWrapper = testRailClientWrapper;
            _caseContentBuilder = caseContentBuilder;
            _sectionSynchronizer = sectionSynchronizer;
            _context = context;
            _tagIndentation = new string(' ', _config.FormattingSettings.TagIndentation);
        }

        public void Sync(List<IFeatureFile> featureFiles)
        {
            Log.Info($"# Start synchronization with TestRail");
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
                    var tagId = scenario.Tags.FirstOrDefault(tag => tag.Name.Contains(_config.TagIdPrefix));

                    var caseRequest = _caseContentBuilder.BuildCaseRequest(scenario, featureFile, featureFileSectionId);

                    // Create test case for feature file that first time sync with TestRail, no tag id present.  
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
                        var formattedTagId = _tagIndentation + _config.TagIdPrefix + addCaseResponse.Id;
                        TextFilesEditMethods.InsertLineToTheFile(featureFile.AbsolutePath, lineNumberToInsert,
                            formattedTagId);
                        insertedTagIds++;
                    }

                    // Update scenarios that have tag id
                    if (tagId is not null)
                    {
                        var caseId = UInt64.Parse(Regex.Match(tagId.Name, @"\d+").Value);
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
                            var formattedTagId = _tagIndentation + _config.TagIdPrefix + testRailCase.Id;
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
            var differentTagIds = testRailTagIds.Except(featureFilesTagIds).ToArray();
            //TODO: asked TestRail support. When parameter soft=1 testcase shouldn't be deleted permanently. 
            try
            {
                _testRailClientWrapper.DeleteCases(differentTagIds);
            }
            catch (TestRailException e)
            {
                Log.Error(e, $"The cases has not been deleted: {string.Join(", ", differentTagIds)}");
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