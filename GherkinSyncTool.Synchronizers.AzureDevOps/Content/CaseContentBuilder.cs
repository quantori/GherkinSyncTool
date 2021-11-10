﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using DataTable = Gherkin.Ast.DataTable;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Content
{
    public class CaseContentBuilder
    {
        private readonly ITestBaseHelper _testBaseHelper;
        private readonly AzureDevopsSettings _azureDevopsSettings = ConfigurationManager.GetConfiguration<AzureDevopsConfigs>().AzureDevopsSettings;

        public CaseContentBuilder(ITestBaseHelper testBaseHelper)
        {
            _testBaseHelper = testBaseHelper;
        }

        public JsonPatchDocument BuildTestCaseDocument(Scenario scenario, IFeatureFile featureFile, int id)
        {
            JsonPatchDocument patchDocument = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Title}",
                    Value = scenario.Name
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Description}",
                    Value = BuildDescription(scenario, featureFile)
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/{WorkItemFields.Id}",
                    Value = id
                }
            };

            AddAreaPathToJsonDocument(patchDocument);
            AddTestStepsToJsonDocument(patchDocument, scenario, featureFile);
            AddTestTagsToJsonDocument(patchDocument, scenario, featureFile);
            AddParametersToJsonDocument(patchDocument, scenario);

            return patchDocument;
        }

        private void AddAreaPathToJsonDocument(JsonPatchDocument patchDocument)
        {
            if (!string.IsNullOrWhiteSpace(_azureDevopsSettings.Area))
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.AreaPath}",
                    Value = _azureDevopsSettings.Area   
                });
            }
        }

        private void AddParametersToJsonDocument(JsonPatchDocument patchDocument, Scenario scenario)
        {
            if(!scenario.Examples.Any()) return;
            
            var patchDocumentParameters = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Parameters}",
                    Value = GetXmlTestParameters(scenario)
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.LocalDataSource}",
                    Value = GetXmlTestParametersValues(scenario)
                }
            };
            
            patchDocument.AddRange(patchDocumentParameters);
        }

        private void AddTestTagsToJsonDocument(JsonPatchDocument patchDocument, Scenario scenario, IFeatureFile featureFile)
        {
            var tags = ConvertToStringTags(scenario, featureFile);
            
            if (!string.IsNullOrWhiteSpace(tags))
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Tags}",
                    Value = tags
                });
            }
        }

        private void AddTestStepsToJsonDocument(JsonPatchDocument jsonPatchDocument, Scenario scenario, IFeatureFile featureFile)
        {
            var steps = GetStepsFromFeatureFile(scenario, featureFile);
            var testBase = _testBaseHelper.Create();
            foreach (var step in steps)
            {
                var testStep = testBase.CreateTestStep();
                testStep.Title = step;
                testBase.Actions.Add(testStep);    
            }
            
            testBase.SaveActions(jsonPatchDocument);
        }

        private List<string> GetStepsFromFeatureFile(Scenario scenario, IFeatureFile featureFile)
        {
            var testParameters = GetTestParameters(scenario);
            
            var scenarioSteps = ExtractStepsFromScenario(scenario.Steps.ToList(), testParameters);

            var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
            if (background is not null)
            {
                var backgroundSteps = ExtractStepsFromScenario(background.Steps.ToList(), testParameters);

                for (var index = 0; index < backgroundSteps.Count; index++)
                {
                    backgroundSteps[index] = $"Background: {backgroundSteps[index]}";
                }

                return backgroundSteps.Concat(scenarioSteps).ToList();
            }

            return scenarioSteps;
        }
        
        private List<string> ExtractStepsFromScenario(List<Step> steps, TestParameters testParameters)
        {
            List<string> resultSteps = new List<string>();
            foreach (var step in steps)
            {
                var keywordFormatted = $"<span style=\"color:RoyalBlue\">{step.Keyword.Trim()} </span>";

                var stepFormatted = FormatTestParameters(testParameters, step.Text);
                
                var fullStep = keywordFormatted + stepFormatted;
                
                if (step.Argument is DocString docString)
                {
                    var stepArgument = FormatTestParameters(testParameters, docString.Content);
                    fullStep += Environment.NewLine + stepArgument;
                }
                
                if (step.Argument is DataTable table)
                {
                    fullStep += Environment.NewLine + BuildTable(table.Rows.ToList(), testParameters);
                }

                resultSteps.Add(fullStep);
            }

            return resultSteps;
        }

        private static string FormatTestParameters(TestParameters testParameters, string stringWithParameters)
        {
            //Azure DevOps test parameters don't allow white spaces.
            foreach (var param in testParameters.Param)
            {
                stringWithParameters =
                    stringWithParameters.Replace($"<{param.Name}>", $"<{param.Name.Replace(" ", string.Empty)}>");
            }
            
            stringWithParameters = stringWithParameters.Replace("<","<span style=\"color:LightSeaGreen\">@").Replace(">","</span>");

            return stringWithParameters;
        }

        private string BuildTable(List<TableRow> tableRows, TestParameters testParameters)
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
                    table.Append($"<td style=\"border: 1px solid RoyalBlue; \">{FormatTestParameters(testParameters, cell.Value)}</td>");
                }

                table.Append("</tr>");
            }
            table.Append("</table>");

            return table.ToString();
        }
        
        private string BuildDescription(Scenario scenario, IFeatureFile featureFile)
        {
            var description = new StringBuilder();
            description.Append($"<p><b>Feature file: </b>{featureFile.RelativePath}</p>");
            description.Append($"<p><b>{featureFile.Document.Feature.Keyword}:</b> {featureFile.Document.Feature.Name}</p>");
            description.Append($"<p>{featureFile.Document.Feature.Description}</p>");
            description.Append($"<p><b>{scenario.Keyword}:</b> {scenario.Name}");
            description.Append($@"<p>{scenario.Description}</p>");

            return description.ToString();
        }
        
        private string GetXmlTestParameters(Scenario scenario)
        {
            var testParameters = GetTestParameters(scenario);
            
            //Azure DevOps test parameters don't allow white spaces.
            for (var index = 0; index < testParameters.Param.Count; index++)
            {
                testParameters.Param[index].Name = testParameters.Param[index].Name.Replace(" ", string.Empty);
            }

            var xmlSerializer = new XmlSerializer(typeof(TestParameters));

            using TextWriter stringWriter = new StringWriter();
            xmlSerializer.Serialize(stringWriter, testParameters);
            return stringWriter.ToString();
        }
        
        private TestParameters GetTestParameters(Scenario scenario)
        {
            var examples = scenario.Examples.ToList();
            if (!examples.Any()) throw new ArgumentNullException(nameof(scenario));

            var testParameters = new TestParameters {Param = new List<Param>()};
            
            foreach (var headerCell in examples.First().TableHeader.Cells)
            {
                testParameters.Param.Add(new Param{Name = headerCell.Value});
            }
            
            return testParameters;
        }
        
        private string GetXmlTestParametersValues(Scenario scenario)
        {
            var examples = scenario.Examples.ToList();
            if (!examples.Any()) throw new ArgumentNullException(nameof(scenario));
            
            var xmlSerializer = new XmlSerializer(typeof(DataSet));
            var dataSet = new DataSet("NewDataSet");
            var dataTable = new System.Data.DataTable("Table1");
            
            var testParameters = GetTestParameters(scenario);
            
            foreach (var param in testParameters.Param)
            {
                //Azure DevOps test parameters don't allow white spaces.
                var dataColumn = new DataColumn(param.Name.Replace(" ", string.Empty));
                dataTable.Columns.Add(dataColumn);
            }
            dataSet.Tables.Add(dataTable);

            foreach (var example in examples)
            {
                foreach (var tableRow in example.TableBody)
                {
                    var dataRow = dataTable.NewRow();
                    var tableCells = tableRow.Cells.ToList();
                    for (int i = 0; i < tableCells.Count; i++)
                    {
                        dataRow[i] = tableCells[i].Value;
                    }

                    dataTable.Rows.Add(dataRow);
                }
            }

            using TextWriter textWriter = new StringWriter();
            xmlSerializer.Serialize(textWriter, dataSet);
        
            return textWriter.ToString();
        }
        
        private string ConvertToStringTags(Scenario scenario, IFeatureFile featureFile)
        {
            var allTags = GherkinFileHelper.GetAllTags(scenario, featureFile);

            return allTags.Any() ? string.Join("; ", allTags.Select(tag => tag.Name.Substring(1))) : null;
        }
    }
}