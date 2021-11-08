using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Content
{
    public class CaseContentBuilder
    {
        private readonly ITestBaseHelper _testBaseHelper;

        public CaseContentBuilder(ITestBaseHelper testBaseHelper)
        {
            _testBaseHelper = testBaseHelper;
        }

        public JsonPatchDocument BuildTestCaseDocument(Scenario scenario, IFeatureFile featureFile, int id)
        {
            JsonPatchDocument patchDocument = new JsonPatchDocument
            {
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Title}",
                    Value = scenario.Name
                },
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Description}",
                    Value = featureFile.RelativePath
                },
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/{WorkItemFields.Id}",
                    Value = id
                }
            };

            var result = AddTestStepsToJsonDocument(patchDocument, GetStepsFromFeatureFile(scenario, featureFile));
            return result;
        }

        private JsonPatchDocument AddTestStepsToJsonDocument(JsonPatchDocument jsonPatchDocument, List<string> steps)
        {
            var testBase = _testBaseHelper.Create();
            foreach (var step in steps)
            {
                ITestStep testStep = testBase.CreateTestStep();
                testStep.Title = step;
                testBase.Actions.Add(testStep);    
            }
            
            return testBase.SaveActions(jsonPatchDocument);
        }

        private List<string> GetStepsFromFeatureFile(Scenario scenario, IFeatureFile featureFile)
        {
            var scenarioSteps = ExtractStepsFromScenario(scenario.Steps.ToList());

            var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
            if (background is not null)
            {
                var backgroundSteps = ExtractStepsFromScenario(background.Steps.ToList());
                return backgroundSteps.Concat(scenarioSteps).ToList();
            }

            return scenarioSteps;
        }
        
        private List<string> ExtractStepsFromScenario(List<Step> steps)
        {
            List<string> resultSteps = new List<string>();
            foreach (var step in steps)
            {
                var keywordFormatted = $"<span style=\"color:RoyalBlue\">{step.Keyword.Trim()} </span>";
                var stepFormatted = step.Text.Replace("<","<span style=\"color:PowderBlue\">&lt").Replace(">","&gt</span>");
                
                var fullStep = keywordFormatted + stepFormatted;
                
                if (step.Argument is DocString docString)
                {
                    fullStep += Environment.NewLine + docString.Content;
                }
                
                if (step.Argument is DataTable table)
                {
                    fullStep += Environment.NewLine + BuildTable(table.Rows.ToList());
                }

                resultSteps.Add(fullStep);
            }

            return resultSteps;
        }
        
        private string BuildTable(List<TableRow> tableRows)
        {
            var table = new StringBuilder();
            table.Append("<table style=\"width: 100%;\">");
            
            //Header
            table.Append("<tr>");
            foreach (var cell in tableRows.First().Cells)
            {
                table.Append($"<th style=\"border: 1px solid RoyalBlue; font-weight: bold;\">{cell.Value}</th>");
            }
            table.Append("</tr>");
            
            //Table body
            for (int i = 1; i < tableRows.Count; i++)
            {
                table.Append("<tr>");
                
                var row = tableRows[i];
                foreach (var cell in row.Cells)
                {
                    table.Append($"<td style=\"border: 1px solid RoyalBlue; \">{cell.Value}</td>");
                }

                table.Append("</tr>");
            }
            table.Append("</table>");

            return table.ToString();
        }
    }
}