using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using GherkinSyncTool.Models;
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
    public class AllureClient
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

        private readonly AllureTestOpsSettings _azureDevopsSettings =
            ConfigurationManager.GetConfiguration<AllureTestOpsConfigs>().AllureTestOpsSettings;

        private readonly Context _context;
        private readonly IAllureClient _allureClient;

        public AllureClient(Context context)
        {
            _context = context;
            _allureClient = RestService.For<IAllureClient>(_azureDevopsSettings.BaseUrl, new RefitSettings
            {
                AuthorizationHeaderValueGetter = () => Task.FromResult(_azureDevopsSettings.AccessToken),
                ContentSerializer = new NewtonsoftJsonContentSerializer(
                    new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        NullValueHandling = NullValueHandling.Ignore
                    }
                )
                
            });
        }

        public IEnumerable<Quantori.AllureTestOpsClient.Model.Content> GetAllTestCases()
        {
            var allContent = new List<Quantori.AllureTestOpsClient.Model.Content>();
            var isLastElementOnThePage = false;
            var page = 0;
            while (!isLastElementOnThePage)
            {
                var response = _allureClient.GetTestCasesAsync(_azureDevopsSettings.ProjectId, page).Result;
                ValidateResponse(response);
                page++;
                isLastElementOnThePage = response.Content!.Last;
                allContent.AddRange(response.Content!.Content);
            }

            return allContent;
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
            return response.Content;
        }


        public TestCaseOverview GetTestCaseOverview(ulong id)
        {
            var response = _allureClient.GetTestCaseOverviewAsync(id).Result;
            ValidateResponse(response);
            return response.Content;
        }

        public void UpdateTestCase(Quantori.AllureTestOpsClient.Model.Content currentCase, TestCaseRequest caseToUpdate)
        {
            if (!IsTestCaseContentEqual(currentCase, caseToUpdate))
            {
                
                var response = _allureClient.UpdateTestCaseAsync(currentCase.Id, (UpdateTestCaseRequest) caseToUpdate).Result;
 
                ValidateResponse(response);
            
                Log.Info($"Updated: [{currentCase.Id}] {currentCase.Name}");
            }
            else
            {
                Log.Info($"Up-to-date: [{currentCase.Id}] {currentCase.Name}");
            }
        }
        
        private static bool IsTestCaseContentEqual(Quantori.AllureTestOpsClient.Model.Content currentCase, TestCaseRequest caseToUpdate)
        {
            if (!currentCase.Name.Equals(caseToUpdate.Name)) return false;
            return true;
        }
    }
}