﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly GherkinSyncToolConfig _gherkinSyncToolConfig = ConfigurationManager.GetConfiguration<GherkinSyncToolConfig>();

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
                TemplateId = templateId
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
                var keywordFormatted = $"__{step.Keyword.Trim()}__ ";
                var stepFormatted = step.Text.Replace("<","___").Replace(">","___");
                
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
            return steps.Select(step => new CustomStepsSeparated {Content = step}).ToList();
        }

        private string ConvertToStringTags(Scenario scenario, IFeatureFile featureFile)
        {
            var allTags = GherkinHelper.GetAllTags(scenario, featureFile);
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
                    if (!string.IsNullOrWhiteSpace(example.Description))
                    {
                        preconditions.AppendLine(example.Description);   
                    }
                    
                    if(example.TableHeader is null) continue;
                    
                    var tableRows = new List<TableRow> {example.TableHeader};
                    tableRows.AddRange(example.TableBody);
                    preconditions.AppendLine(ConvertToStringTable(tableRows));
                }
            }
            return preconditions.ToString();
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
    }
}