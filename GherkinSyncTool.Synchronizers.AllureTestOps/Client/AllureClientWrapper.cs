using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.AllureTestOps.Exception;
using GherkinSyncTool.Synchronizers.AllureTestOps.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
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

        public void UpdateTestCase(TestCaseContent currentCase, TestCaseRequest caseToUpdate)
        {
            if (!IsTestCaseContentEqual(currentCase, caseToUpdate))
            {
                var response = _allureClient.UpdateTestCaseAsync(currentCase.Id, caseToUpdate).Result;

                ValidateResponse(response);

                Log.Info($"Updated: [{currentCase.Id}] {caseToUpdate.Name}");
            }
            else
            {
                Log.Info($"Up-to-date: [{currentCase.Id}] {currentCase.Name}");
            }
        }

        public IEnumerable<Status> GetAllStatuses()
        {
            return GetAllContent(i => _allureClient.GetStatusAsync(null,i).Result);
        }

        public IEnumerable<WorkflowSchema> GetAllWorkflowSchemas(int projectId)
        {
            return GetAllContent(i => _allureClient.GetWorkflowSchemaAsync(projectId,i).Result);
        }

        public IEnumerable<WorkflowContent> GetAllWorkflows()
        {
            return GetAllContent(i => _allureClient.GetWorkflowAsync(i).Result);;
        }

        private static bool IsTestCaseContentEqual(TestCaseContent currentCase, TestCaseRequest caseToUpdate)
        {
            if (!currentCase.Name.Equals(caseToUpdate.Name)) return false;
            if (!currentCase.Automated.Equals(caseToUpdate.Automated)) return false;
            if (!currentCase.Status.Id.Equals(caseToUpdate.StatusId)) return false;
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
    }
}