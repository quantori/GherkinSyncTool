using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.AzureDevOps.Model;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using NLog;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Client
{
    public class AzureDevopsClient
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);

        private readonly AzureDevopsSettings _azureDevopsSettings =
            ConfigurationManager.GetConfiguration<AzureDevopsConfigs>().AzureDevopsSettings;

        private readonly VssConnection _connection;

        private readonly Context _context;

        //Azure DevOps batch request limitation
        const int BatchRequestLimit = 200;

        public AzureDevopsClient(Context context)
        {
            var uri = new Uri(_azureDevopsSettings.BaseUrl);
            var personalAccessToken = _azureDevopsSettings.PersonalAccessToken;
            _connection = new VssConnection(uri, new VssBasicCredential(string.Empty, personalAccessToken));
            _context = context;
        }

        public WitBatchRequest BuildCreateTestCaseBatchRequest(JsonPatchDocument patchDocument)
        {
            var workItemTrackingHttpClient = GetWorkItemTrackingHttpClient();
            return workItemTrackingHttpClient.CreateWorkItemBatchRequest(_azureDevopsSettings.Project,
                WorkItemTypes.TestCase, patchDocument, false, false);
        }

        public WitBatchRequest BuildUpdateTestCaseBatchRequest(int id, JsonPatchDocument patchDocument)
        {
            var workItemTrackingHttpClient = GetWorkItemTrackingHttpClient();
            return workItemTrackingHttpClient.CreateWorkItemBatchRequest(id, patchDocument, false, false);
        }

        public IEnumerable<int> GetAllTestCasesIds()
        {
            var workItemTrackingHttpClient = GetWorkItemTrackingHttpClient();

            // wiql - Work Item Query Language
            var wiql = new Wiql
            {
                Query = $@"Select [{WorkItemFields.Id}] 
                           From WorkItems 
                           Where [System.WorkItemType] = '{WorkItemTypes.TestCase}'"
            };

            var workItemIds = workItemTrackingHttpClient.QueryByWiqlAsync(wiql, _azureDevopsSettings.Project).Result;

            return workItemIds.WorkItems.Select(reference => reference.Id);
        }
        
        public IEnumerable<int> GetSyncedTestCasesIds()
        {
            var workItemTrackingHttpClient = GetWorkItemTrackingHttpClient();

            // wiql - Work Item Query Language
            var wiql = new Wiql
            {
                Query = $@"Select [{WorkItemFields.Id}] 
                           From WorkItems 
                           Where [System.WorkItemType] = '{WorkItemTypes.TestCase}' 
                           AND [{WorkItemFields.State}] <> '{TestCaseState.Closed}'
                           AND [{WorkItemFields.Tags}] Contains '{Tags.GherkinSyncToolIdTagPrefix + _azureDevopsSettings.GherkinSyncToolId}'"
            };

            var workItemIds = workItemTrackingHttpClient.QueryByWiqlAsync(wiql, _azureDevopsSettings.Project).Result;

            return workItemIds.WorkItems.Select(reference => reference.Id);
        }

        public List<WorkItem> ExecuteWorkItemBatch(List<WitBatchRequest> request)
        {
            var result = new List<WorkItem>();

            if (request.Count > BatchRequestLimit)
            {
                var requestChunks = request.Batch(BatchRequestLimit);

                foreach (var requestChunk in requestChunks)
                {
                    result.AddRange(SendWorkItemBatch(requestChunk.ToList()));
                }

                return result;
            }

            return SendWorkItemBatch(request);
        }

        public List<WorkItem> GetWorkItemsBatch(IEnumerable<int> ids, IEnumerable<string> fields = null)
        {
            var result = new List<WorkItem>();
            var idsList = ids.ToList();

            if (idsList.Count > BatchRequestLimit)
            {
                var requestChunks = idsList.Batch(BatchRequestLimit);

                foreach (var requestChunk in requestChunks)
                {
                    result.AddRange(GetWorkItems(requestChunk, fields));
                }

                return result;
            }

            return GetWorkItems(idsList);
        }

        private List<WorkItem> GetWorkItems(IEnumerable<int> ids, IEnumerable<string> fields = null)
        {
            var workItemsList = new List<WorkItem>();
            try
            {
                var workItemTrackingHttpClient = GetWorkItemTrackingHttpClient();
                workItemsList = workItemTrackingHttpClient.GetWorkItemsAsync(_azureDevopsSettings.Project, ids, fields)
                    .Result;
            }
            catch (Exception e)
            {
                Log.Error(e, "Error executing get work items request");
                _context.IsRunSuccessful = false;
            }

            return workItemsList;
        }

        private List<WorkItem> SendWorkItemBatch(List<WitBatchRequest> request)
        {
            var result = new List<WorkItem>();
            try
            {
                var workItemTrackingHttpClient = GetWorkItemTrackingHttpClient();
                var workItemBatchResponseList = workItemTrackingHttpClient.ExecuteBatchRequest(request).Result;

                for (var i = 0; i < workItemBatchResponseList.Count; i++)
                {
                    var witBatchResponse = workItemBatchResponseList[i];

                    if (witBatchResponse.Code != 200)
                    {
                        Log.Error($"Something went wrong with the test case synchronization. Title: {request[i].GetFields()[WorkItemFields.Title]}");
                        Log.Error($"Status code: {witBatchResponse.Code}{Environment.NewLine}Body: {witBatchResponse.Body}");

                        _context.IsRunSuccessful = false;
                        continue;
                    }

                    var workItem = witBatchResponse.ParseBody<WorkItem>();
                    result.Add(workItem);

                    if (workItem.Rev != 1)
                    {
                        if (workItem.Fields[WorkItemFields.State].Equals(TestCaseState.Closed))
                        {
                            Log.Info($"Closed: [{workItem.Id}] {workItem.Fields[WorkItemFields.Title]}");
                            continue;
                        }

                        Log.Info($"Updated: [{workItem.Id}] {workItem.Fields[WorkItemFields.Title]}");
                        continue;
                    }

                    Log.Info($"Created: [{workItem.Id}] {workItem.Fields[WorkItemFields.Title]}");
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error executing work item batch request");
                _context.IsRunSuccessful = false;
            }

            return result;
        }

        private WorkItemTrackingHttpClient GetWorkItemTrackingHttpClient()
        {
            WorkItemTrackingHttpClient workItemTrackingHttpClient;
            try
            {
                workItemTrackingHttpClient = _connection.GetClient<WorkItemTrackingHttpClient>();
            }
            catch (Exception)
            {
                Log.Error("Azure DevOps server connection issue, please check configs.");
                throw;
            }

            return workItemTrackingHttpClient;
        }
    }
}