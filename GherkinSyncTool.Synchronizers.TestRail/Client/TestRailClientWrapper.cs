using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using GherkinSyncTool.Models.Configuration;
using GherkinSyncTool.Synchronizers.TestRail.Exceptions;
using GherkinSyncTool.Synchronizers.TestRail.Model;
using Newtonsoft.Json.Linq;
using NLog;
using Polly;
using Polly.Retry;
using TestRail;
using TestRail.Types;
using TestRail.Utils;

namespace GherkinSyncTool.Synchronizers.TestRail.Client
{
    public class TestRailClientWrapper
    {
        private static readonly Logger Log = LogManager.GetLogger(MethodBase.GetCurrentMethod()?.DeclaringType?.Name);
        private readonly TestRailClient _testRailClient;
        private readonly TestRailSettings _testRailSettings = ConfigurationManager.GetConfiguration<TestRailConfigs>().TestRailSettings;
        private int _requestsCount;

        public TestRailClientWrapper()
        {
            _testRailClient = new TestRailClient(_testRailSettings.BaseUrl,
                _testRailSettings.UserName, _testRailSettings.Password);
        }

        public Case AddCase(CaseRequest caseRequest)
        {
            var policy = CreateResultHandlerPolicy<Case>();

            var addCaseResponse = policy.Execute(() =>
                _testRailClient.AddCase(caseRequest.SectionId, caseRequest.Title, null, caseRequest.PriorityId, null, null, caseRequest.References,
                    caseRequest.JObjectCustomFields, caseRequest.TemplateId));

            ValidateRequestResult(addCaseResponse);

            Log.Info($"Created: [{addCaseResponse.Payload.Id}] {addCaseResponse.Payload.Title}");
            return addCaseResponse.Payload;
        }

        public void UpdateCase(Case currentCase, CaseRequest caseToUpdate)
        {
            var policy = CreateResultHandlerPolicy<Case>();
            var caseId = currentCase.Id ??
                         throw new ArgumentException("Case Id cannot be null");
            if (!IsTestCaseContentEqual(caseToUpdate, currentCase))
            {
                var updateCaseResult = policy.Execute(() =>
                    _testRailClient.UpdateCase(caseId, caseToUpdate.Title, null, caseToUpdate.PriorityId, null, null, caseToUpdate.References,
                        caseToUpdate.JObjectCustomFields, caseToUpdate.TemplateId));

                ValidateRequestResult(updateCaseResult);

                Log.Info($"Updated: [{caseId}] {caseToUpdate.Title}");
            }
            else
            {
                Log.Info($"Up-to-date: [{caseId}] {caseToUpdate.Title}");
            }
        }

        public IList<Case> GetCases()
        {
            var policy = CreateResultHandlerPolicy<IList<Case>>();
            var cases = policy.Execute(() =>
                _testRailClient.GetCases(_testRailSettings.ProjectId, _testRailSettings.SuiteId, null, _testRailSettings.TemplateId));

            ValidateRequestResult(cases);

            var gherkinToolCases = cases.Payload.Where(c =>
            {
                var caseCustomFields = c.JsonFromResponse.ToObject<CaseCustomFields>();
                return caseCustomFields.GherkinSyncToolId is not null &&
                       caseCustomFields.GherkinSyncToolId.Equals(_testRailSettings.GherkinSyncToolId);
            });

            return gherkinToolCases.ToList();
        }

        public void DeleteCases(List<ulong> caseIds)
        {
            if (!caseIds.Any()) return;
            Log.Debug("Deleting scenarios which are not exist.");
            var policy = CreateResultHandlerPolicy<BaseTestRailType>();
            var cases = policy.Execute(() =>
                _testRailClient.DeleteCases(_testRailSettings.ProjectId, caseIds, _testRailSettings.SuiteId, true));

            ValidateRequestResult(cases);

            Log.Info($"Deleted cases: {string.Join(", ", caseIds)}");
        }

        public ulong? CreateSection(CreateSectionRequest request)
        {
            var policy = CreateResultHandlerPolicy<Section>();

            var response = policy.Execute(() =>
                _testRailClient.AddSection(
                    request.ProjectId,
                    request.SuiteId,
                    request.Name,
                    request.ParentId,
                    request.Description));

            ValidateRequestResult(response);
            Log.Info($"Section created: [{response.Payload.Id}] {response.Payload.Name}");

            return response.Payload.Id;
        }

        public IEnumerable<Section> GetSections(ulong projectId)
        {
            var policy = CreateResultHandlerPolicy<IList<Section>>();
            var result = policy.Execute(() => _testRailClient.GetSections(projectId));
            ValidateRequestResult(result);
            return result.Payload;
        }

        private void ValidateRequestResult<T>(RequestResult<T> requestResult)
        {
            if (requestResult.StatusCode != HttpStatusCode.OK)
            {
                if (!string.IsNullOrEmpty(requestResult.RawJson) &&
                    requestResult.RawJson.Contains("not a valid test case"))
                    throw new TestRailNoCaseException("Case not found", requestResult.ThrownException);

                throw new TestRailException(
                    $"There is an issue with requesting TestRail: {requestResult.StatusCode.ToString()} " +
                    $"{Environment.NewLine}{requestResult.RawJson}",
                    requestResult.ThrownException);
            }

            Log.Debug($"Requests sent: {++_requestsCount}");
        }

        /// <summary>
        /// Moves feature files to new section
        /// </summary>
        /// <param name="newSectionId">Id of destination section</param>
        /// <param name="caseIds">collection of feature file id's</param>
        public void MoveCases(ulong newSectionId, IEnumerable<ulong> caseIds)
        {
            Log.Debug("Moving testcases to new sections.");
            var policy = CreateResultHandlerPolicy<BaseTestRailType>();
            var result = policy.Execute(() =>
                _testRailClient.MoveCases(newSectionId, caseIds));
            ValidateRequestResult(result);
            Log.Info($"Moved cases: {string.Join(", ", caseIds)} to section {newSectionId}");
        }

        public void MoveSection(ulong sectionId, ulong? parentId = null, ulong? afterId = null)
        {
            Log.Debug("Moving section");
            var policy = CreateResultHandlerPolicy<Section>();
            var result = policy.Execute(() =>
                _testRailClient.MoveSection(sectionId, parentId, afterId));
            ValidateRequestResult(result);
            Log.Info($"Section moved: [{result.Payload.Id}] {result.Payload.Name}");
        }

        public IEnumerable<CaseField> GetCaseFields()
        {
            Log.Debug("Getting case fields");
            var policy = CreateResultHandlerPolicy<IList<CaseField>>();
            var caseFields = policy.Execute(() =>
                _testRailClient.GetCaseFields());
            ValidateRequestResult(caseFields);
            return caseFields.Payload;
        }

        public IEnumerable<Priority> GetCasePriorities()
        {
            Log.Debug("Getting case priorities");
            var policy = CreateResultHandlerPolicy<IList<Priority>>();
            var priorities = policy.Execute(() =>
                _testRailClient.GetPriorities());
            ValidateRequestResult(priorities);
            return priorities.Payload;
        }

        /// <summary>
        /// RetryPolicy for request result of given type 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private RetryPolicy<RequestResult<T>> CreateResultHandlerPolicy<T>()
        {
            return Policy.HandleResult<RequestResult<T>>(r => (int)r.StatusCode < 200 || (int)r.StatusCode > 299)
                .WaitAndRetry(_testRailSettings.RetriesCount, retryAttempt =>
                {
                    Log.Debug(
                        $"Attempt {retryAttempt} of {_testRailSettings.RetriesCount}, waiting for {_testRailSettings.PauseBetweenRetriesSeconds} seconds");
                    return TimeSpan.FromSeconds(_testRailSettings.PauseBetweenRetriesSeconds);
                });
        }

        private static bool IsTestCaseContentEqual(CaseRequest caseRequest, Case testRailCase)
        {
            if (!testRailCase.Title.Equals(caseRequest.Title)) return false;
            if (!testRailCase.TemplateId.Equals(caseRequest.TemplateId)) return false;

            if (string.IsNullOrEmpty(testRailCase.References) && !string.IsNullOrEmpty(caseRequest.References)) return false;
            if (!string.IsNullOrEmpty(testRailCase.References) && string.IsNullOrEmpty(caseRequest.References)) return false;
            if (!string.IsNullOrEmpty(testRailCase.References) && !string.IsNullOrEmpty(caseRequest.References))
            {
                if (!testRailCase.References!.Equals(caseRequest.References)) return false;
            }

            if (!testRailCase.PriorityId.Equals((ulong?)caseRequest.PriorityId)) return false;

            var testRailCaseCustomFields = testRailCase.JsonFromResponse.ToObject<CaseCustomFields>();
            if (!JToken.DeepEquals(caseRequest.JObjectCustomFields, JObject.FromObject(testRailCaseCustomFields))) return false;

            return true;
        }
    }
}