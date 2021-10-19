using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gherkin.Ast;
using GherkinSyncTool.Configuration;
using GherkinSyncTool.Interfaces;
using GherkinSyncTool.Synchronizers.AzureDevopsSynchronizer.Model;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace GherkinSyncTool.Synchronizers.AzureDevopsSynchronizer.Content
{
    public class CaseContentBuilder
    {
        private readonly GherkinSyncToolConfig _config = ConfigurationManager.GetConfiguration();

        public JsonPatchDocument BuildTestCaseDocument(Scenario scenario, IFeatureFile featureFile, int id)
        {
            var steps = GetSteps(scenario, featureFile);
            JsonPatchDocument patchDocument = new JsonPatchDocument();
            
            patchDocument.Add(
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + WorkItemFields.Title,
                    Value = scenario.Name
                }
            );
            patchDocument.Add(
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + WorkItemFields.Description,
                    Value = featureFile.RelativePath
                }
            );
            patchDocument.Add(
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = "/" + WorkItemFields.Id,
                    Value = id
                }
            );
            
            return patchDocument;
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
                var fullStep = $"**{step.Keyword.Trim()}** " + step.Text.Replace("<","***").Replace(">","***");
                
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


        private string ConvertToStringTags(Scenario scenario, IFeatureFile featureFile)
        {
            List<Tag> allTags = new List<Tag>();

            var featureTags = featureFile.Document.Feature.Tags.ToList();
            if (featureTags.Any())
            {
                allTags.AddRange(featureTags);
            }

            var scenarioTags = scenario.Tags.ToList();
            if (scenarioTags.Any())
            {
                allTags.AddRange(scenarioTags);
            }

            if (scenario.Examples != null && scenario.Examples.Any())
            {
                foreach (var example in scenario.Examples)
                {
                    if (example.Tags != null && example.Tags.Any())
                    {
                        allTags.AddRange(example.Tags);
                    }
                }
            }

            allTags.RemoveAll(tag => tag.Name.Contains(_config.TagIdPrefix));

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