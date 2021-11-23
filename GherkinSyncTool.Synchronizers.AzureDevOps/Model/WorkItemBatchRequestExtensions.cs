using System;
using System.Collections.Generic;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;

namespace GherkinSyncTool.Synchronizers.AzureDevOps.Model
{
    public static class WorkItemBatchRequestExtensions
    {
        public static Dictionary<string, string> GetFields(this WitBatchRequest witBatchRequest)
        {
            var witBatchRequestBody = JsonConvert.DeserializeObject<List<WorkItemBatchRequestBody>>(witBatchRequest.Body);
            if (witBatchRequestBody is null) throw new NullReferenceException();

            var fieldsToUpdateFeatureFile = new Dictionary<string, string>();

            foreach (var item in witBatchRequestBody)
            {
                fieldsToUpdateFeatureFile.Add(item.Path.Replace("/fields/", ""), item.Value);
            }

            return fieldsToUpdateFeatureFile;
        }
    }
}