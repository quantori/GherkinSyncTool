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

        public TestRailSynchronizer(TestRailClientWrapper testRailClientWrapper, CaseContentBuilder caseContentBuilder,
            SectionSynchronizer sectionSynchronizer)
        {
            _testRailClientWrapper = testRailClientWrapper;
            _caseContentBuilder = caseContentBuilder;
            _sectionSynchronizer = sectionSynchronizer;
            _tagIndentation = new string(' ', _config.FormattingSettings.TagIndentation);
        }

        public void Sync(List<IFeatureFile> featureFiles)
        {
            Log.Info($"# Start synchronization with TestRail");
            var stopwatch = Stopwatch.StartNew();
            var casesToMove = new Dictionary<ulong, List<ulong>>(); 
            foreach (var featureFile in featureFiles)
            {
                var insertedTagIds = 0;
                var featureFileSectionId = _sectionSynchronizer.GetOrCreateSectionIdFromPath(featureFile.RelativePath);
                foreach (var scenario in featureFile.Document.Feature.Children.OfType<Scenario>())
                {
                    var tagId = scenario.Tags.FirstOrDefault(tag => tag.Name.Contains(_config.TagIdPrefix));

                    var caseRequest = _caseContentBuilder.BuildCaseRequest(scenario, featureFile, featureFileSectionId);
                    //Feature file that first time sync with TestRail, no tag id present.  
                    if (tagId is null)
                    {
                        var addCaseResponse = _testRailClientWrapper.AddCase(caseRequest);

                        var lineNumberToInsert = scenario.Location.Line - 1 + insertedTagIds;
                        var formattedTagId = _tagIndentation + _config.TagIdPrefix + addCaseResponse.Id;
                        TextFilesEditMethods.InsertLineToTheFile(featureFile.AbsolutePath, lineNumberToInsert, formattedTagId);
                        insertedTagIds++;
                    }
                    //Update scenarios that have tag id
                    if (tagId is not null)
                    {
                        var caseId = UInt64.Parse(Regex.Match(tagId.Name, @"\d+").Value);
                        Case testRailCase;
                        try
                        {
                            testRailCase = _testRailClientWrapper.GetCase(caseId);
                            _testRailClientWrapper.UpdateCase(testRailCase, caseRequest);
                        }
                        catch (TestRailNoCaseException e)
                        {
                            var message = string.IsNullOrEmpty(e.Message) ? "unknown" : e.Message;
                            Log.Warn($"Case with id {caseId} not found. Reason: {message} Recreating missing case");
                            testRailCase = _testRailClientWrapper.AddCase(caseRequest);
                            TextFilesEditMethods.ReplaceLineInTheFile(featureFile.AbsolutePath, caseId.ToString(),testRailCase.Id.ToString());
                        }
                        var testRailSectionId = testRailCase.SectionId;
                        AddCasesToMove(testRailSectionId, featureFileSectionId, caseId, casesToMove);
                    }
                }
            }
            //Moving cases to new sections
            foreach (var (key, value) in casesToMove)
            {   
                _testRailClientWrapper.MoveCases(key, value);
            }
            Log.Info(@$"Synchronization with TestRail finished in: {stopwatch.Elapsed:mm\:ss\.fff}");
        }

        /// <summary>
        /// Adds cases to move to new section, if changed
        /// </summary>
        /// <param name="oldSectionId">id of old section</param>
        /// <param name="currentSectionId">id of current section</param>
        /// <param name="caseId">case id</param>
        /// <param name="casesToMove">IDictionary where key is section id and the value is case ids collection</param>
        private void AddCasesToMove(ulong? oldSectionId, ulong? currentSectionId, ulong caseId, IDictionary<ulong, List<ulong>> casesToMove)
        {
            if (oldSectionId.HasValue && currentSectionId.HasValue &&
                !oldSectionId.Equals(currentSectionId))
            {
                var key = currentSectionId.Value;
                if (!casesToMove.ContainsKey(key))
                    casesToMove.Add(key, new List<ulong>() { caseId });
                else casesToMove[key].Add(caseId);
            }
        }
    }
}