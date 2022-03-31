using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.TestRail.Model;

namespace GherkinSyncTool.Synchronizers.TestRail.Content
{
    public class CaseContentBuilder
    {
        private readonly TestRailSettings _testRailSettings = ConfigurationManager.GetConfiguration<TestRailConfigs>().TestRailSettings;

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
                    GherkinSyncToolId = _testRailSettings.GherkinSyncToolId
                },
                TemplateId = templateId,
                References = ConvertToStringReferences(scenario, featureFile),
            };
            return createCaseRequest;
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
                var stepFormatted = step.Text.Replace("<","***").Replace(">","***");
                
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
            allTags.RemoveAll(tag => tag.Name.Contains("@Reference:", StringComparison.InvariantCultureIgnoreCase));
            return allTags.Any() ? string.Join(", ", allTags.Select(tag => tag.Name.Substring(1))) : null;
        }

        private string ConvertToStringPreconditions(Scenario scenario, IFeatureFile featureFile)
        {
            var preconditions = new StringBuilder();
            preconditions.AppendLine($"## {featureFile.Document.Feature.Keyword}: {featureFile.Document.Feature.Name}");
            preconditions.AppendLine(featureFile.Document.Feature.Description);
            preconditions.AppendLine($"## {scenario.Keyword}: {scenario.Name}");
            preconditions.AppendLine(scenario.Description);
            
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
                    
                    if(example.TableHeader is null) continue;
                    
                    var tableRows = new List<TableRow> {example.TableHeader};
                    tableRows.AddRange(example.TableBody);
                    preconditions.AppendLine(ConvertToStringTable(tableRows));
                }
            }
            
            //Testrail Api convert ' to html encoded
            return preconditions.ToString().Replace("'","&#39;");
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
            var refList = allTags.Where(tag => tag.Name.Contains("@Reference:", StringComparison.InvariantCultureIgnoreCase)).ToList();
            
            if (refList.Any())
            {
                result = string.Join(",", refList.Select(x => x.Name.Replace("@Reference:","", StringComparison.InvariantCultureIgnoreCase)));
            }

            return result;
        }
    }
}