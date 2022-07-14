using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Models.Utils;
using GherkinSyncTool.Synchronizers.AzureDevOps.Client;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using GherkinSyncTool.Synchronizers.AzureDevOps.Utils;
using Microsoft.TeamFoundation.TestManagement.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using DataTable = Gherkin.Ast.DataTable;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Content
{
    public class CaseContentBuilder
    {
        private readonly ITestBaseHelper _testBaseHelper;

        private readonly AzureDevopsSettings _azureDevopsSettings = ConfigurationManager.GetConfiguration<AzureDevopsConfigs>().AzureDevopsSettings;

        private readonly AzureDevopsClient _devopsClient;

        public CaseContentBuilder(ITestBaseHelper testBaseHelper, AzureDevopsClient devopsClient)
        {
            _testBaseHelper = testBaseHelper;
            _devopsClient = devopsClient;
        }

        private JsonPatchDocument BuildTestCaseDocument(Scenario scenario, IFeatureFile featureFile, int? id = null)
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

        public WitBatchRequest BuildTestCaseBatchRequest(Scenario scenario, IFeatureFile featureFile, int? id = null)
        {
            var patchDocument = BuildTestCaseDocument(scenario, featureFile, id);

            return _devopsClient.BuildCreateTestCaseBatchRequest(patchDocument);
        }

        public WitBatchRequest BuildUpdateTestCaseBatchRequest(Scenario scenario, IFeatureFile featureFile, int id)
        {
            var patchDocument = BuildTestCaseDocument(scenario, featureFile);
            AddStateToJsonDocument(patchDocument, TestCaseState.Design);

            if (!scenario.Examples.Any()) RemoveParametersFromJsonDocument(patchDocument);

            return _devopsClient.BuildUpdateTestCaseBatchRequest(id, patchDocument);
        }

        public WitBatchRequest BuildUpdateStateBatchRequest(int id, string state)
        {
            if (string.IsNullOrWhiteSpace(state)) throw new ArgumentNullException(nameof(state));

            var patchDocument = new JsonPatchDocument();
            AddStateToJsonDocument(patchDocument, state);

            return _devopsClient.BuildUpdateTestCaseBatchRequest(id, patchDocument);
        }

        private void AddStateToJsonDocument(JsonPatchDocument patchDocument, string state)
        {
            if (string.IsNullOrWhiteSpace(state)) throw new ArgumentNullException(nameof(state));

            patchDocument.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = $"/fields/{WorkItemFields.State}",
                Value = state
            });
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

        private void RemoveParametersFromJsonDocument(JsonPatchDocument patchDocument)
        {
            var patchDocumentParameters = new JsonPatchDocument
            {
                new()
                {
                    Operation = Operation.Remove,
                    Path = $"/fields/{WorkItemFields.Parameters}"
                },
                new()
                {
                    Operation = Operation.Remove,
                    Path = $"/fields/{WorkItemFields.LocalDataSource}"
                }
            };

            patchDocument.AddRange(patchDocumentParameters);
        }

        private void AddTestTagsToJsonDocument(JsonPatchDocument patchDocument, Scenario scenario,
            IFeatureFile featureFile)
        {
            var tags = ConvertToStringTags(scenario, featureFile);

            var gherkinSyncToolIdTag = Tags.GherkinSyncToolIdTagPrefix + _azureDevopsSettings.GherkinSyncToolId;

            tags = string.IsNullOrWhiteSpace(tags) ? gherkinSyncToolIdTag : $"{tags}; {gherkinSyncToolIdTag}";

            patchDocument.Add(new JsonPatchOperation
            {
                Operation = Operation.Replace,
                Path = $"/fields/{WorkItemFields.Tags}",
                Value = tags
            });
        }

        private void AddTestStepsToJsonDocument(JsonPatchDocument jsonPatchDocument, Scenario scenario,
            IFeatureFile featureFile)
        {
            var steps = GetStepsFromFeatureFile(scenario, featureFile);
            var testBase = _testBaseHelper.Create();
            foreach (var step in steps)
            {
                var testStep = testBase.CreateTestStep();
                if (_azureDevopsSettings.SetThenStepsAsExpected && step.Key.Contains("Then", StringComparison.InvariantCultureIgnoreCase))
                {
                    testStep.ExpectedResult = step.Value;
                }
                else testStep.Title = step.Value;

                testBase.Actions.Add(testStep);
            }

            testBase.SaveActions(jsonPatchDocument);
        }

        private List<KeyValuePair<string, string>> GetStepsFromFeatureFile(Scenario scenario, IFeatureFile featureFile)
        {
            var testParameters = GetTestParameters(scenario.Examples);

            var scenarioSteps = ExtractStepsFromScenario(scenario.Steps.ToList(), testParameters);

            var background = featureFile.Document.Feature.Children.OfType<Background>().FirstOrDefault();
            if (background is not null)
            {
                var backgroundSteps = ExtractStepsFromScenario(background.Steps.ToList(), testParameters);

                for (var index = 0; index < backgroundSteps.Count; index++)
                {
                    backgroundSteps[index] = new KeyValuePair<string, string>(backgroundSteps[index].Key,
                        $"<span style=\"color:RoyalBlue\">Background:</span> {backgroundSteps[index].Value}");
                }

                return backgroundSteps.Concat(scenarioSteps).ToList();
            }

            return scenarioSteps;
        }

        /// <summary>
        /// Get and format the steps list from the feature file scenario.
        /// </summary>
        /// <param name="steps"></param>
        /// <param name="testParameters"></param>
        /// <returns>List of step.Keyword + fully formatted step</returns>
        private List<KeyValuePair<string, string>> ExtractStepsFromScenario(List<Step> steps, TestParameters testParameters)
        {
            var resultSteps = new List<KeyValuePair<string, string>>();
            var stepKeywordTmp = string.Empty;
            foreach (var step in steps)
            {
                var stepKeyword = step.Keyword.Trim();
                var keywordFormatted = $"<span style=\"color:RoyalBlue\">{stepKeyword}</span>";

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
                
                if (stepKeyword.Equals("And", StringComparison.InvariantCultureIgnoreCase))
                {
                    stepKeyword = stepKeywordTmp;
                }
                stepKeywordTmp = stepKeyword;
                resultSteps.Add(new KeyValuePair<string, string>(stepKeyword, fullStep));
            }

            return resultSteps;
        }

        private string FormatMultilineArgument(string multilineArgument, TestParameters testParameters)
        {
            var multilineArgumentFormatted = FormatTestParameters(testParameters, multilineArgument);
            multilineArgumentFormatted = Regex.Replace(multilineArgumentFormatted, @"\n|\r\n", "<br>");

            return $"<p>{multilineArgumentFormatted}</p>";
        }

        /// <summary>
        /// Format test parameters with Azure Devops API requirements.
        /// </summary>
        /// <param name="testParameters"></param>
        /// <param name="stringWithParameters"></param>
        /// <param name="addSpaces">Add space before and after parameter</param>
        /// <returns></returns>
        private static string FormatTestParameters(TestParameters testParameters, string stringWithParameters, bool addSpaces = false)
        {
            stringWithParameters = stringWithParameters.EncodeHtml();

            if (testParameters is null)
            {
                return stringWithParameters;
            }

            foreach (var param in testParameters.Param)
            {
                var parameter = $"&lt;{param.Name}&gt;";

                //Azure DevOps test parameters required a whitespace or non-word character at the end. Parameters that go one by one without space are not permissible.
                var indexOfParameter = stringWithParameters.IndexOf(parameter, StringComparison.InvariantCulture);
                //Continue if the parameter is not in the string.
                if (indexOfParameter == -1) continue;

                var theNextCharacterAfterParameter = indexOfParameter + parameter.Length;

                //In case line stringWithParameters contains only a single parameter.
                if (theNextCharacterAfterParameter < stringWithParameters.Length)
                {
                    if (!Regex.IsMatch(stringWithParameters[theNextCharacterAfterParameter].ToString(), @"\W+|\s") ||
                        stringWithParameters[theNextCharacterAfterParameter].Equals('<'))
                    {
                        stringWithParameters = stringWithParameters.Insert(theNextCharacterAfterParameter, " ");
                    }
                }

                var spaceBefore = addSpaces && indexOfParameter == 0 ? " " : string.Empty;
                var spaceAfter = addSpaces && theNextCharacterAfterParameter == stringWithParameters.Length ? " " : string.Empty;

                //Azure DevOps test parameters don't allow white spaces in a middle of a parameter.
                stringWithParameters = stringWithParameters.Replace(parameter,
                    $"<span style=\"color:LightSeaGreen\">{spaceBefore}@{param.Name.FormatStringToCamelCase()}{spaceAfter}</span>");
            }

            return stringWithParameters;
        }

        private string BuildTable(List<TableRow> tableRows, TestParameters testParameters)
        {
            var table = new StringBuilder();
            table.Append("<br><table style=\"width: 100%; table-layout: fixed; \">");

            //Header
            table.Append("<tr>");
            foreach (var cell in tableRows.First().Cells)
            {
                //Azure DevOps work properly with parameters in the tables in case a parameter begins from a space.
                var header = FormatTestParameters(testParameters, cell.Value, true);
                table.Append($"<th style=\"border: 1px solid RoyalBlue; font-weight: bold; padding: 3px;\">{header}</th>");
            }

            table.Append("</tr>");

            //Table body
            for (int i = 1; i < tableRows.Count; i++)
            {
                table.Append("<tr>");

                var row = tableRows[i];
                foreach (var cell in row.Cells)
                {
                    var data = FormatTestParameters(testParameters, cell.Value, true);
                    table.Append($"<td style=\"border: 1px solid RoyalBlue; padding: 3px; \">{data}</td>");
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
            description.Append($"<p><b>{featureFile.Document.Feature.Keyword}:</b> {featureFile.Document.Feature.Name.EncodeHtml()}</p>");

            if (!string.IsNullOrWhiteSpace(featureFile.Document.Feature.Description))
            {
                var featureDescriptionFormatted = featureFile.Document.Feature.Description.EncodeHtml();
                featureDescriptionFormatted = Regex.Replace(featureDescriptionFormatted, @"\n|\r\n", "<br>");
                description.Append($"<p>{featureDescriptionFormatted}</p>");
            }
            
            var background = featureFile.Document.Feature.Children.OfType<Background>().SingleOrDefault();

            if (!string.IsNullOrWhiteSpace(background?.Name))
            {
                description.Append($"<p><b>{background.Keyword}:</b> {background.Name.EncodeHtml()}</p>");
            }

            if (!string.IsNullOrWhiteSpace(background?.Description))
            {
                if (string.IsNullOrWhiteSpace(background.Name))
                {
                    description.Append($"<p><b>{background.Keyword}:</b></p>");
                }

                var backgroundDescriptionFormatted = background.Description.EncodeHtml();
                backgroundDescriptionFormatted = Regex.Replace(backgroundDescriptionFormatted, @"\n|\r\n", "<br>");
                description.Append($"<p>{backgroundDescriptionFormatted}</p>");
            }

            description.Append($"<p><b>{scenario.Keyword}:</b> {scenario.Name.EncodeHtml()}</p>");

            if (!string.IsNullOrWhiteSpace(scenario.Description))
            {
                var scenarioDescriptionFormatted = scenario.Description.EncodeHtml();
                scenarioDescriptionFormatted = Regex.Replace(scenarioDescriptionFormatted, @"\n|\r\n", "<br>");
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
                testParameters.Param[index].Name = testParameters.Param[index].Name.FormatStringToCamelCase();
            }

            var emptyNamespaces = new XmlSerializerNamespaces(new[] { XmlQualifiedName.Empty });
            var serializer = new XmlSerializer(typeof(TestParameters));
            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true
            };

            using var stream = new StringWriter();
            using var writer = XmlWriter.Create(stream, settings);

            serializer.Serialize(writer, testParameters, emptyNamespaces);
            return stream.ToString();
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

            var dataSet = new DataSet("NewDataSet");
            dataSet.Locale = new CultureInfo(string.Empty);
            var dataTable = new System.Data.DataTable("Table1");

            var testParameters = GetTestParameters(examplesList);

            foreach (var param in testParameters.Param)
            {
                //Azure DevOps test parameters don't allow white spaces.
                var dataColumn = new DataColumn(param.Name.FormatStringToCamelCase());
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

            var settings = new XmlWriterSettings
            {
                Indent = true,
                OmitXmlDeclaration = true,
                CheckCharacters = true
            };

            using var stream = new StringWriter();
            using var writer = XmlWriter.Create(stream, settings);

            dataSet.WriteXml(writer, XmlWriteMode.WriteSchema);

            return stream.ToString();
        }

        private string ConvertToStringTags(Scenario scenario, IFeatureFile featureFile)
        {
            var allTags = GherkinHelper.GetAllTags(scenario, featureFile);

            return allTags.Any() ? string.Join("; ", allTags.Select(tag => tag.Name.Substring(1))) : null;
        }
    }
}