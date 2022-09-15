using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.AllureTestOps.Exception;
using GherkinSyncTool.Synchronizers.AllureTestOps.Model;
using NLog;
using Quantori.AllureTestOpsClient;
using Quantori.AllureTestOpsClient.Model;
using Refit;

namespace GherkinSyncTool.Synchronizers.AllureTestOps.Client
{
    public class AllureClientWrapper
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

        private readonly AllureTestOpsSettings _azureDevopsSettings =
            ConfigurationManager.GetConfiguration<AllureTestOpsConfigs>().AllureTestOpsSettings;

        private readonly IAllureClient _allureClient;

        public AllureClientWrapper()
        {
            _allureClient = AllureClient.Get(_azureDevopsSettings.BaseUrl, _azureDevopsSettings.AccessToken);
        }

        public IEnumerable<TestCaseContent> GetAllTestCases()
        {
            return GetAllContent(i => _allureClient.GetTestCasesAsync(_azureDevopsSettings.ProjectId, i).Result);
        }

        private void ValidateResponse(IApiResponse response)
        {
            if (!response.IsSuccessStatusCode)
            {
                Log.Error(response.Error, response.Error?.Content);
                throw new AllureException(response.Error?.ReasonPhrase);
            }

            if (response.Error is not null)
            {
                Log.Error(response.Error.Message + Environment.NewLine + response.Error.InnerException);
                throw new System.Exception(response.Error.Message);
            }
        }

        public TestCase AddTestCase(CreateTestCaseRequest caseRequest)
        {
            var response = _allureClient.CreateTestCaseAsync(caseRequest).Result;
            ValidateResponse(response);
            Log.Info($"Created: [{response.Content!.Id}] {response.Content.Name}");
            return response.Content;
        }

        public TestCaseOverview GetTestCaseOverview(ulong id)
        {
            var response = _allureClient.GetTestCaseOverviewAsync(id).Result;
            ValidateResponse(response);
            return response.Content;
        }

        public void UpdateTestCase(TestCaseContent currentCase, CreateTestCaseRequestExtended caseToUpdate)
        {
            var testCaseOverview = _allureClient.GetTestCaseOverviewAsync(currentCase.Id).Result.Content;
            var contentEqual = IsTestCaseContentEqual(testCaseOverview, caseToUpdate);
            var scenarioEqual = IsScenarioEqual(testCaseOverview, caseToUpdate);
            if (!contentEqual || !scenarioEqual)
            {
                var response = _allureClient.UpdateTestCaseAsync(currentCase.Id, caseToUpdate.CreateTestCaseRequest).Result;

                ValidateResponse(response);

                Log.Info($"Updated: [{currentCase.Id}] {caseToUpdate.CreateTestCaseRequest.Name}");
                return;
            }
            //Remove scenario
            if (testCaseOverview!.Scenario is not null && caseToUpdate.CreateTestCaseRequest.Scenario is null)
            {
                var response = _allureClient.DeleteTestCaseScenario(testCaseOverview.Id).Result;
                ValidateResponse(response);
                Log.Info($"Updated: [{testCaseOverview.Id}] {caseToUpdate.CreateTestCaseRequest.Name}");
                return;
            }
            
            Log.Info($"Up-to-date: [{currentCase.Id}] {currentCase.Name}");
        }

        private bool IsScenarioEqual(TestCaseOverview testCaseOverview, CreateTestCaseRequestExtended caseToUpdate)
        {
            if (testCaseOverview.Scenario is null && caseToUpdate.CreateTestCaseRequest.Scenario is null)
            {
                return true;
            }
            
            if (testCaseOverview.Scenario is null && caseToUpdate.CreateTestCaseRequest.Scenario.Steps is not null)
            {
                if (caseToUpdate.StepsAttachments.Any())
                {
                    UpdateTestCaseStepAttachments(caseToUpdate, testCaseOverview);
                }
                return false;
            }

            if (testCaseOverview.Scenario!.Steps.Count != caseToUpdate.CreateTestCaseRequest.Scenario.Steps!.Count)
            {
                if (caseToUpdate.StepsAttachments.Any())
                {
                    UpdateTestCaseStepAttachments(caseToUpdate, testCaseOverview);
                }

                return false;
            }

            var stepsFromAllure = testCaseOverview.Scenario.Steps;
            var stepsFromFeature = caseToUpdate.CreateTestCaseRequest.Scenario.Steps;

            for (var i = 0; i < stepsFromFeature.Count; i++)
            {
                if (!stepsFromAllure[i].Attachments.Any() && caseToUpdate.StepsAttachments.ContainsKey(i))
                {
                    UpdateTestCaseStepAttachments(caseToUpdate, testCaseOverview);
                    return false;
                }

                if (stepsFromAllure[i].Attachments.Any() && !caseToUpdate.StepsAttachments.ContainsKey(i))
                {
                    UpdateTestCaseStepAttachments(caseToUpdate, testCaseOverview);
                    return false;
                }

                if (stepsFromAllure[i].Attachments.Any() && caseToUpdate.StepsAttachments.ContainsKey(i))
                {
                    if (stepsFromAllure[i].Attachments.FirstOrDefault()!.ContentLength != System.Text.Encoding.Default.GetString(caseToUpdate.StepsAttachments[i].Value).Length)
                    {
                        UpdateTestCaseStepAttachments(caseToUpdate, testCaseOverview);
                        return false;
                    }
                    
                    //TODO: compare attachment content
                }

                if (!stepsFromAllure[i].Keyword.Equals(stepsFromFeature[i].Keyword) || !stepsFromAllure[i].Name.Equals(stepsFromFeature[i].Name))
                {
                    return false;
                }
            }

            return true;
        }

        public IEnumerable<Status> GetAllStatuses()
        {
            return GetAllContent(i => _allureClient.GetStatusAsync(null, i).Result);
        }

        public IEnumerable<WorkflowSchema> GetAllWorkflowSchemas(int projectId)
        {
            return GetAllContent(i => _allureClient.GetWorkflowSchemaAsync(projectId, i).Result);
        }

        public IEnumerable<WorkflowContent> GetAllWorkflows()
        {
            return GetAllContent(i => _allureClient.GetWorkflowAsync(i).Result);
        }

        public List<Attachment> UploadTestCaseAttachments(long testCaseId, IEnumerable<ByteArrayPart> content)
        {
            var byteArrayParts = content as ByteArrayPart[] ?? content.ToArray();
            if (!byteArrayParts.Any())
            {
                throw new ArgumentException("There are no test case attachments.");
            }

            var response = _allureClient.UploadTestCaseAttachment(testCaseId, byteArrayParts).Result;
            ValidateResponse(response);
            return response.Content;
        }

        public void RemoveTestCaseAttachment(long id)
        {
            var response = _allureClient.DeleteTestCaseAttachment(id).Result;
            ValidateResponse(response);
        }

        public void UploadStepAttachments(CreateTestCaseRequestExtended caseRequestExtended, TestCase caseToUpdate)
        {
            AddTestCaseStepAttachments(caseRequestExtended, caseToUpdate.Id);

            var response = _allureClient.UpdateTestCaseScenario(caseToUpdate.Id, caseRequestExtended.CreateTestCaseRequest.Scenario).Result;

            ValidateResponse(response);
        }

        private void UpdateTestCaseStepAttachments(CreateTestCaseRequestExtended caseToUpdate, TestCaseOverview testCaseOverview)
        {
            var attachment = testCaseOverview.Scenario.Steps.Where(step => step.Attachments.FirstOrDefault() is not null);
            foreach (var id in attachment.Select(step => step.Attachments.FirstOrDefault()!.Id))
            {
                RemoveTestCaseAttachment(id);
            }

            AddTestCaseStepAttachments(caseToUpdate, testCaseOverview.Id);
        }

        private static bool IsTestCaseContentEqual(TestCaseOverview currentCase, CreateTestCaseRequestExtended caseToUpdate)
        {
            if (!currentCase.Name.Equals(caseToUpdate.CreateTestCaseRequest.Name)) return false;
            if (!currentCase.Automated.Equals(caseToUpdate.CreateTestCaseRequest.Automated)) return false;
            if (!currentCase.Status.Id.Equals(caseToUpdate.CreateTestCaseRequest.StatusId)) return false;
            return true;
        }

        private IEnumerable<T> GetAllContent<T>(Func<int, IApiResponse<GetContentResponse<T>>> function)
        {
            var allContent = new List<T>();
            var isLastElementOnThePage = false;
            var page = 0;
            while (!isLastElementOnThePage)
            {
                var response = function(page);

                ValidateResponse(response);
                page++;
                isLastElementOnThePage = response.Content!.Last;
                allContent.AddRange(response.Content!.Content);
            }

            return allContent;
        }

        private void AddTestCaseStepAttachments(CreateTestCaseRequestExtended caseRequestExtended, long testCaseId)
        {
            var attachments = UploadTestCaseAttachments(testCaseId, caseRequestExtended.StepsAttachments.Select(pair => pair.Value));
            var stepNumbers = caseRequestExtended.StepsAttachments.Keys.ToList();
            for (var i = 0; i < stepNumbers.Count; i++)
            {
                var stepNumber = stepNumbers[i];
                caseRequestExtended.CreateTestCaseRequest.Scenario.Steps[stepNumber].Attachments = new List<Attachment> { attachments[i] };
            }
        }
    }
}