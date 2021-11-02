using Gherkin.Ast;
using GherkinSyncTool.Models;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Content
{
    public class CaseContentBuilder
    {
        public JsonPatchDocument BuildTestCaseDocument(Scenario scenario, IFeatureFile featureFile, int id)
        {
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Title}",
                    Value = scenario.Name
                }
            );
            patchDocument.Add(
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/fields/{WorkItemFields.Description}",
                    Value = featureFile.RelativePath
                }
            );
            patchDocument.Add(
                new JsonPatchOperation
                {
                    Operation = Operation.Add,
                    Path = $"/{WorkItemFields.Id}",
                    Value = id
                }
            );

            return patchDocument;
        }
    }
}