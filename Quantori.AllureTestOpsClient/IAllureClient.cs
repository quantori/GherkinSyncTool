﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quantori.AllureTestOpsClient.Model;
using Refit;

namespace Quantori.AllureTestOpsClient
{
    [Headers("accept: */*", "Authorization: Api-Token")]
    public interface IAllureClient
    {
        /// <summary>
        /// Find all test cases for specified project
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="page">Zero-based page index (0..N)</param>
        /// <param name="size">The size of the page to be returned</param>
        /// <param name="sort">Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported.</param>
        /// <returns></returns>
        [Get("/api/rs/testcase")]
        Task<IApiResponse<GetContentResponse<TestCaseContent>>> GetTestCasesAsync(int projectId, int page = 0, int size = 100, string sort = null);

        /// <summary>
        /// Create a new test case
        /// </summary>
        /// <param name="createTestCaseRequest"></param>
        /// <returns></returns>
        [Post("/api/rs/testcase")]
        Task<IApiResponse<TestCase>> CreateTestCaseAsync([Body] CreateTestCaseRequest createTestCaseRequest);

        /// <summary>
        /// Get test case overview
        /// </summary>
        /// <param name="testCaseId"></param>
        /// <returns></returns>
        [Get("/api/rs/testcase/{id}/overview")]
        Task<IApiResponse<TestCaseOverview>> GetTestCaseOverviewAsync([AliasAs("id")] ulong testCaseId);

        /// <summary>
        /// Update a test case
        /// </summary>
        /// <param name="testCaseId"></param>
        /// <param name="updateTestCaseRequest"></param>
        /// <returns></returns>
        [Patch("/api/rs/testcase/{id}")]
        Task<IApiResponse<TestCase>> UpdateTestCaseAsync([AliasAs("id")] ulong testCaseId, [Body] TestCaseRequest updateTestCaseRequest);

        /// <summary>
        /// Find all statuses
        /// </summary>
        /// <returns></returns>
        [Get("/api/rs/status")]
        Task<IApiResponse<GetContentResponse<Status>>> GetStatusAsync(int? workflowId = null, int page = 0, int size = 100, string sort = null);

        /// <summary>
        /// Find all workflow schemas for given project
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="sort"></param>
        [Get("/api/rs/workflowschema")]
        Task<IApiResponse<GetContentResponse<WorkflowSchema>>> GetWorkflowSchemaAsync(int projectId, int page = 0, int size = 100, string sort = null);
        
        /// <summary>
        /// Find all workflow
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        [Get("/api/rs/workflow")]
        Task<IApiResponse<GetContentResponse<WorkflowContent>>> GetWorkflowAsync(int page = 0, int size = 100, string sort = null);
        
        #region Test case attachment

        /// <summary>
        /// Upload new test case attachments
        /// </summary>
        [Multipart]
        [Post("/api/rs/testcase/attachment")]
        Task<IApiResponse<List<Attachment>>> UploadTestCaseAttachmentAsync(long testCaseId, [AliasAs("file")] IEnumerable<ByteArrayPart> attachments);
        
        /// <summary>
        /// Delete test case attachment
        /// </summary>
        [Delete("/api/rs/testcase/attachment/{id}")]
        Task<IApiResponse> DeleteTestCaseAttachmentAsync([AliasAs("id")] long testCaseId);
        
        /// <summary>
        /// Delete test case attachment
        /// </summary>
        [Get("/api/rs/testcase/attachment/{id}/content")]
        Task<IApiResponse<string>> GetTestCaseAttachmentContentAsync([AliasAs("id")] long attachmentId);
        
        #endregion
        
        #region Test case scenario
        
        /// <summary>
        /// Update scenario for test case
        /// </summary>
        [Post("/api/rs/testcase/{id}/scenario")]
        Task<IApiResponse<Scenario>> UpdateTestCaseScenarioAsync([AliasAs("id")] long testCaseId, [Body] Scenario scenario);
        
        /// <summary>
        /// Delete scenario for test case
        /// </summary>
        [Delete("/api/rs/testcase/{id}/scenario")]
        Task<IApiResponse<Scenario>> DeleteTestCaseScenarioAsync([AliasAs("id")] long testCaseId);

        #endregion
    }
}