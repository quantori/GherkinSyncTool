using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

        public JsonPatchDocument BuildTestCaseDocument(Scenario scenario, IFeatureFile featureFile, int? id = null)
        {
            if (string.IsNullOrWhiteSpace(scenario.Name))
            {
                throw new ArgumentNullException($"Scenario title is missing, please check the feature file: {featureFile.RelativePath}");
            }
            
            var patchDocument = new JsonPatchDocument
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
                }
            };

            AddIdToJsonDocument(patchDocument, id);
            AddAreaPathToJsonDocument(patchDocument);
            AddTestStepsToJsonDocument(patchDocument, scenario, featureFile);
            AddTestTagsToJsonDocument(patchDocument, scenario, featureFile);
            AddParametersToJsonDocument(patchDocument, scenario);

            return patchDocument;
        }

        private void AddIdToJsonDocument(JsonPatchDocument patchDocument, int? id)
        {
            if (id is null) return;
            {
                patchDocument.Add(new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/{WorkItemFields.Id}",
                    Value = id
                });
            }
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
            if (!scenario.Examples.Any()) return;

            var patchDocumentParameters = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Parameters}",
                    Value = GetXmlTestParameters(scenario.Examples)
                },
                new()
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.LocalDataSource}",
                    Value = GetXmlTestParametersValues(scenario.Examples)
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
                    Operation = Operation.Replace,
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
            var testParameters = GetTestParameters(scenario.Examples);

            var scenarioSteps = ExtractStepsFromScenario(scenario.Steps.ToList(), testParameters);

            var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
            if (background is not null)
            {
                var backgroundSteps = ExtractStepsFromScenario(background.Steps.ToList(), testParameters);

                for (var index = 0; index < backgroundSteps.Count; index++)
                {
                    backgroundSteps[index] = $"<span style=\"color:RoyalBlue\">Background:</span> {backgroundSteps[index]}";
                }

                return backgroundSteps.Concat(scenarioSteps).ToList();
            }

            return scenarioSteps;
        }

        private List<string> ExtractStepsFromScenario(List<Step> steps, TestParameters testParameters)
        {
            var resultSteps = new List<string>();
            foreach (var step in steps)
            {
                var keywordFormatted = $"<span style=\"color:RoyalBlue\">{step.Keyword.Trim()}</span>";

                var stepFormatted = FormatTestParameters(testParameters, step.Text);

                var fullStep = $"{keywordFormatted} {stepFormatted}";

                if (step.Argument is DocString docString)
                {
                    var stepArgument = FormatMultilineArgument(docString.Content, testParameters);
                    fullStep += stepArgument;
                }

                if (step.Argument is DataTable table)
                {
                    fullStep += BuildTable(table.Rows.ToList(), testParameters);
                }

                resultSteps.Add(fullStep);
            }

            return resultSteps;
        }

        private string FormatMultilineArgument(string multilineArgument, TestParameters testParameters)
        {
            var multilineArgumentFormatted = FormatTestParameters(testParameters, multilineArgument);
            multilineArgumentFormatted = Regex.Replace(multilineArgumentFormatted, @"\n|\r\n", "<br>");

            return $"<p>{multilineArgumentFormatted}</p>";
        }

        private static string FormatTestParameters(TestParameters testParameters, string stringWithParameters)
        {
            if (testParameters is null) return stringWithParameters;

            foreach (var param in testParameters.Param)
            {
                var parameter = $"<{param.Name}>";

                //Azure DevOps test parameters required a whitespace or non-word character at the end. Parameters that go one by one without space are not permissible.
                var indexOfParameter = stringWithParameters.IndexOf(parameter, StringComparison.InvariantCulture);
                if (indexOfParameter == -1) continue;

                var theNextCharacterAfterParameter = indexOfParameter + parameter.Length;

                //In case line stringWithParameters contains only a single parameter
                if (theNextCharacterAfterParameter < stringWithParameters.Length)
                {
                    if (!Regex.IsMatch(stringWithParameters[theNextCharacterAfterParameter].ToString(), @"\W+|\s") || stringWithParameters[theNextCharacterAfterParameter].Equals('<'))
                    {
                        stringWithParameters = stringWithParameters.Insert(theNextCharacterAfterParameter, " ");
                    }
                }

                //Azure DevOps test parameters don't allow white spaces.
                stringWithParameters = stringWithParameters.Replace($"<{param.Name}>", $"<span style=\"color:LightSeaGreen\">@{param.Name.Replace(" ", string.Empty)}</span>");
            }

            return stringWithParameters;
        }

        private string BuildTable(List<TableRow> tableRows, TestParameters testParameters)
        {
            var table = new StringBuilder();
            table.Append("<br><table style=\"width: 100%;\">");

            //Header
            table.Append("<tr>");
            foreach (var cell in tableRows.First().Cells)
            {
                table.Append($"<th style=\"border: 1px solid RoyalBlue; font-weight: bold;\">{FormatTestParameters(testParameters, cell.Value)}</th>");
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
            
            //The path will be used for inserting tag id to a feature file
            description.Append($"<p><div id={HtmlTagIds.FeatureFilePathId}><b>Feature file: </b>{featureFile.RelativePath}</div></p>");
            description.Append($"<p><b>{featureFile.Document.Feature.Keyword}:</b> {featureFile.Document.Feature.Name}</p>");

            if (!string.IsNullOrWhiteSpace(featureFile.Document.Feature.Description))
            {
                var featureDescriptionFormatted =
                    Regex.Replace(featureFile.Document.Feature.Description, @"\n|\r\n", "<br>");
                description.Append($"<p>{featureDescriptionFormatted}</p>");
            }

            description.Append($"<p><b>{scenario.Keyword}:</b> {scenario.Name}</p>");

            if (!string.IsNullOrWhiteSpace(scenario.Description))
            {
                var scenarioDescriptionFormatted = Regex.Replace(scenario.Description, @"\n|\r\n", "<br>");
                description.Append($@"<p>{scenarioDescriptionFormatted}</p>");
            }

            return description.ToString();
        }

        private string GetXmlTestParameters(IEnumerable<Examples> examples)
        {
            var examplesList = examples.ToList();
            if (!examplesList.Any()) throw new ArgumentNullException(nameof(examples));

            var testParameters = GetTestParameters(examplesList);

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

        private TestParameters GetTestParameters(IEnumerable<Examples> examples)
        {
            var examplesList = examples.ToList();
            if (!examplesList.Any()) return null;

            var testParameters = new TestParameters { Param = new List<Param>() };

            foreach (var headerCell in examplesList.First().TableHeader.Cells)
            {
                testParameters.Param.Add(new Param { Name = headerCell.Value });
            }

            return testParameters;
        }

        private string GetXmlTestParametersValues(IEnumerable<Examples> examples)
        {
            var examplesList = examples.ToList();
            if (!examplesList.Any()) throw new ArgumentNullException(nameof(examples));

            var xmlSerializer = new XmlSerializer(typeof(DataSet));
            var dataSet = new DataSet("NewDataSet");
            var dataTable = new System.Data.DataTable("Table1");

            var testParameters = GetTestParameters(examplesList);

            foreach (var param in testParameters.Param)
            {
                //Azure DevOps test parameters don't allow white spaces.
                var dataColumn = new DataColumn(param.Name.Replace(" ", string.Empty));
                dataTable.Columns.Add(dataColumn);
            }

            dataSet.Tables.Add(dataTable);

            foreach (var example in examplesList)
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
            var allTags = GherkinHelper.GetAllTags(scenario, featureFile);

            return allTags.Any() ? string.Join("; ", allTags.Select(tag => tag.Name.Substring(1))) : null;
        }
    }
}