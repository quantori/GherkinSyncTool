using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GherkinSyncTool.Models;
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
        private readonly string _uri = "https://dev.azure.com/GherkinSyncTool";
        private readonly string _personalAccessToken = "m7ek4hcv6m43qwrho7xj2ibs42xzfgjh3tarcw3xrho737zztjuq";
        private readonly string _project = "Test project";
        private readonly VssConnection _connection;
        private readonly Context _context;

        public AzureDevopsClient(Context context)
        {
            Uri uri = new Uri(_uri);
            string personalAccessToken = _personalAccessToken;
            _connection = new VssConnection(uri, new VssBasicCredential(string.Empty, personalAccessToken));
            _context = context;
        }

        public WitBatchRequest CreateTestCaseBatchRequest(JsonPatchDocument patchDocument)
        {
            var workItemTrackingHttpClient = _connection.GetClient<WorkItemTrackingHttpClient>();
            return workItemTrackingHttpClient.CreateWorkItemBatchRequest(_project, WorkItemTypes.TestCase, patchDocument, false, false);
        }

        public WitBatchRequest UpdateTestCaseBatchRequest(int id, JsonPatchDocument patchDocument)
        {
            var workItemTrackingHttpClient = _connection.GetClient<WorkItemTrackingHttpClient>();
            return workItemTrackingHttpClient.CreateWorkItemBatchRequest(id, patchDocument, false, false);
        }

        public List<WorkItem> ExecuteWorkItemBatch(List<WitBatchRequest> request)
        {
            const int batchRequestLimit = 200;
            var result = new List<WorkItem>();
            
            if (request.Count > batchRequestLimit)
            {
                var requestChunks = request.Batch(batchRequestLimit);

                foreach (var requestChunk in requestChunks)
                {
                    result.AddRange(SendWorkItemBatch(requestChunk.ToList()));
                }
                return result;
            }
            return SendWorkItemBatch(request);
        }

        private List<WorkItem> SendWorkItemBatch(List<WitBatchRequest> request)
        {
            var result = new List<WorkItem>();
            try
            {
                var workItemTrackingHttpClient = _connection.GetClient<WorkItemTrackingHttpClient>();
                var workItemBatchResponseList = workItemTrackingHttpClient.ExecuteBatchRequest(request).Result;

                foreach (var witBatchResponse in workItemBatchResponseList)
                {
                    if (witBatchResponse.Code == 200)
                    {
                        var workItem = witBatchResponse.ParseBody<WorkItem>();
                        result.Add(workItem);
                        Log.Info($"Created: [{workItem.Id}] {workItem.Fields[WorkItemFields.Title]}");
                    }
                    else
                    {
                        Log.Error($"Something went wrong with creating the test case. Status code: {witBatchResponse.Code}{Environment.NewLine}Body: {witBatchResponse.Body}");
                        _context.IsRunSuccessful = false;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error executing work item batch request");
                _context.IsRunSuccessful = false;
            }

            return result;
        }
    }
}