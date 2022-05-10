using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.TestRail.Model;
using GherkinSyncTool.Synchronizers.TestRail.Utils;
using NLog;

namespace GherkinSyncTool.Synchronizers.TestRail.Content
{
    public class CaseContentBuilder
    {
        private readonly TestRailSettings _testRailSettings = ConfigurationManager.GetConfiguration<TestRailConfigs>().TestRailSettings;
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly Context _context;
        private readonly TestRailCaseFields _testRailCaseFields;

        private Dictionary<uint, string> _testRailAutomationTypesDir;
        public Dictionary<uint, string> TestRailAutomationTypesDir => _testRailAutomationTypesDir ??= GetTestRailAutomationTypes();

        public CaseContentBuilder(Context context, TestRailCaseFields testRailCaseFields)
        {
            _context = context;
            _testRailCaseFields = testRailCaseFields;
        }

        public CaseRequest BuildCaseRequest(Scenario scenario, IFeatureFile featureFile, ulong sectionId)
        {
            var steps = GetSteps(scenario, featureFile);

            var templateId = _testRailSettings.TemplateId;

            var createCaseRequest = new CaseRequest
            {
                Title = scenario.Name,
                SectionId = sectionId,
                CustomFields = new CaseCustomFields
                {
                    Preconditions = ConvertToStringPreconditions(scenario, featureFile),
                    StepsSeparated = ConvertToCustomStepsSeparated(steps),
                    Tags = ConvertToStringTags(scenario, featureFile),
                    GherkinSyncToolId = _testRailSettings.GherkinSyncToolId,
                    AutomationType = ConvertToIntAutomationType(scenario, featureFile)
                },
                TemplateId = templateId,
                References = ConvertToStringReferences(scenario, featureFile),
                PriorityId = ConvertToPriorityId(scenario, featureFile)
            };
            return createCaseRequest;
        }

        private uint? ConvertToIntAutomationType(Scenario scenario, IFeatureFile featureFile)
        {
            var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
            var automatedTag = allTags.LastOrDefault(tag => tag.Name.Contains(TagsConstants.Automation, StringComparison.InvariantCultureIgnoreCase));
            
            uint? result = null;
            if (automatedTag is not null)
            {
                var automatedString = automatedTag.Name.Replace(TagsConstants.Automation, "", StringComparison.InvariantCultureIgnoreCase);
                try
                {
                    result = TestRailAutomationTypesDir.First(pair => pair.Value.Equals(automatedString, StringComparison.InvariantCultureIgnoreCase)).Key;
                }
                catch (InvalidOperationException e)
                {
                    var automationTypesNames = string.Join(", ", TestRailAutomationTypesDir.Values);
                    Log.Error(e, $"'{automatedString}' is incorrect automation type for scenario: '{scenario.Name}'. Valid automation types are: {automationTypesNames}.");
                    _context.IsRunSuccessful = false;
                }
            }

            return result;
        }

        private ulong? ConvertToPriorityId(Scenario scenario, IFeatureFile featureFile)
        {
            var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
            var priorityTag = allTags.LastOrDefault(tag => tag.Name.Contains(TagsConstants.Priority, StringComparison.InvariantCultureIgnoreCase));
            
            var defaultPriority = _testRailCaseFields.CasePriorities
                .Where(priority => priority.IsDefault)
                .Select(priority => priority.Id).FirstOrDefault();
            
            if (priorityTag is not null)
            {
                var priorityString = priorityTag.Name.Replace(TagsConstants.Priority, "", StringComparison.InvariantCultureIgnoreCase);

                var result = _testRailCaseFields.CasePriorities
                    .Where(priority => priority.Name.Equals(priorityString, StringComparison.CurrentCultureIgnoreCase))
                    .Select(priority => priority.Id).FirstOrDefault();
                if (result != 0)
                {
                    return result;
                }
                    
                var priorityNames = string.Join(", ", _testRailCaseFields.CasePriorities.Select(priority => priority.Name));
                Log.Error($"'{priorityString}' is incorrect priority for scenario: '{scenario.Name}'. Valid priorities are: {priorityNames}.");
                _context.IsRunSuccessful = false;
                    
                return defaultPriority;
            }

            return defaultPriority;

        }

        private List<string> GetSteps(Scenario scenario, IFeatureFile featureFile)
        {
            var scenarioSteps = ExtractSteps(scenario.Steps.ToList());

            var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
            if (background is not null)
            {
                var backgroundSteps = ExtractSteps(background.Steps.ToList());
                return backgroundSteps.Concat(scenarioSteps).ToList();
            }

            return scenarioSteps;
        }

        private List<string> ExtractSteps(List<Step> steps)
        {
            List<string> resultSteps = new List<string>();
            foreach (var step in steps)
            {
                var keywordFormatted = $"**{step.Keyword.Trim()}** ";
                var stepFormatted = step.Text.Replace("<", "***").Replace(">", "***");

                var fullStep = keywordFormatted + stepFormatted;

                if (step.Argument is DocString docString)
                {
                    fullStep += Environment.NewLine + docString.Content;
                }

                if (step.Argument is DataTable table)
                {
                    fullStep += Environment.NewLine + ConvertToStringTable(table.Rows.ToList());
                }

                resultSteps.Add(fullStep);
            }

            return resultSteps;
        }

        private List<CustomStepsSeparated> ConvertToCustomStepsSeparated(List<string> steps)
        {
            return steps.Select(step => new CustomStepsSeparated
            {
                //Testrail Api convert ' to html encoded
                Content = step.Replace("'","&#39;")
                
            }).ToList();
        }

        private string ConvertToStringTags(Scenario scenario, IFeatureFile featureFile)
        {
            var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
            //Remove references from tags to not duplicate with the reference field
            allTags.RemoveAll(tag => tag.Name.Contains(TagsConstants.Reference, StringComparison.InvariantCultureIgnoreCase));
            //Remove priority from tags to not duplicate with the reference field
            allTags.RemoveAll(tag => tag.Name.Contains(TagsConstants.Priority, StringComparison.InvariantCultureIgnoreCase));
            //Remove automation type from tags to not duplicate with the automation type field
            allTags.RemoveAll(tag => tag.Name.Contains(TagsConstants.Automation, StringComparison.InvariantCultureIgnoreCase));
            return allTags.Any() ? string.Join(", ", allTags.Select(tag => tag.Name.Substring(1))) : null;
        }

        private string ConvertToStringPreconditions(Scenario scenario, IFeatureFile featureFile)
        {
            var preconditions = new StringBuilder();
            preconditions.AppendLine($"## {featureFile.Document.Feature.Keyword}: {featureFile.Document.Feature.Name}");
            preconditions.AppendLine(featureFile.Document.Feature.Description);
            preconditions.AppendLine($"## {scenario.Keyword}: {scenario.Name}");
            preconditions.AppendLine(scenario.Description);
            
            var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
            if (background is not null && (!string.IsNullOrWhiteSpace(background.Name) || !string.IsNullOrWhiteSpace(background.Description)))
            {
                preconditions.AppendLine($"## {background.Keyword}: {background.Name}");
                preconditions.AppendLine(background.Description);
            }

            var examples = scenario.Examples.ToList();
            if (examples.Any())
            {
                foreach (var example in examples)
                {
                    preconditions.AppendLine($"## {example.Keyword}: {example.Name}");
                    if (!string.IsNullOrEmpty(example.Description))
                    {
                        preconditions.AppendLine(example.Description);
                    }

                    if (example.TableHeader is null) continue;

                    var tableRows = new List<TableRow> { example.TableHeader };
                    tableRows.AddRange(example.TableBody);
                    preconditions.AppendLine(ConvertToStringTable(tableRows));
                }
            }

            //Testrail Api convert ' to html encoded
            return preconditions.ToString().Replace("'", "&#39;");
        }

        private string ConvertToStringTable(List<TableRow> tableRows)
        {
            var table = new StringBuilder();
            table.Append("||");

            //Header
            foreach (var cell in tableRows.First().Cells)
            {
                table.Append($"|:{cell.Value}");
            }

            table.AppendLine();

            //Table body
            for (int i = 1; i < tableRows.Count; i++)
            {
                table.Append("|");

                var row = tableRows[i];
                foreach (var cell in row.Cells)
                {
                    table.Append($"|{cell.Value}");
                }

                table.AppendLine();
            }

            return table.ToString();
        }

        private string ConvertToStringReferences(Scenario scenario, IFeatureFile featureFile)
        {
            string result = null;
            var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
            var refList = allTags.Where(tag => tag.Name.Contains(TagsConstants.Reference, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (refList.Any())
            {
                result = string.Join(",", refList.Select(x => x.Name.Replace(TagsConstants.Reference, "", StringComparison.InvariantCultureIgnoreCase)));
            }

            return result;
        }

        private Dictionary<uint, string> GetTestRailAutomationTypes()
        {
            var result = new Dictionary<uint, string>();
            //Get possible automation types
            var automationTypes = _testRailCaseFields.CaseFields.FirstOrDefault(field => field.SystemName.Equals("custom_automation_type"));
            
            var automationTypesString = automationTypes?.JsonFromResponse.ToObject<CustomFieldsModel>().Configs.Select(config => config.Options.Items)
                .FirstOrDefault();
            
            if (automationTypesString is not null)
            {
                var list = automationTypesString.Split("\n");
                foreach (var item in list)
                {
                    var automationType = item.Split(",");
                    result!.Add(uint.Parse(automationType[0]), automationType[1].Trim());
                }
            }

            return result;
        }
    }
}