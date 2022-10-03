using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Quantori.AllureTestOpsClient.Model;
using Refit;

namespace Quantori.AllureTestOpsClient
{
    [Headers("accept: */*", "Authorization: Api-Token")]
    public interface IAllureClient
    {
        #region Test case controller

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
        Task<IApiResponse<TestCaseOverview>> GetTestCaseOverviewAsync([AliasAs("id")] long testCaseId);

        /// <summary>
        /// Update a test case
        /// </summary>
        /// <param name="testCaseId"></param>
        /// <param name="updateTestCaseRequest"></param>
        /// <returns></returns>
        [Patch("/api/rs/testcase/{id}")]
        Task<IApiResponse<TestCase>> UpdateTestCaseAsync([AliasAs("id")] long testCaseId, [Body] TestCaseRequest updateTestCaseRequest);

        #endregion

        #region Status controller

        /// <summary>
        /// Find all statuses
        /// </summary>
        /// <returns></returns>
        [Get("/api/rs/status")]
        Task<IApiResponse<GetContentResponse<Status>>> GetStatusAsync(int? workflowId = null, int page = 0, int size = 100, string sort = null);

        #endregion

        #region Workflow schema controller

        /// <summary>
        /// Find all workflow schemas for given project
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="sort"></param>
        [Get("/api/rs/workflowschema")]
        Task<IApiResponse<GetContentResponse<WorkflowSchema>>>
            GetWorkflowSchemaAsync(int projectId, int page = 0, int size = 100, string sort = null);

        #endregion

        #region Workflow controller

        /// <summary>
        /// Find all workflow
        /// </summary>
        /// <param name="page"></param>
        /// <param name="size"></param>
        /// <param name="sort"></param>
        /// <returns></returns>
        [Get("/api/rs/workflow")]
        Task<IApiResponse<GetContentResponse<WorkflowContent>>> GetWorkflowAsync(int page = 0, int size = 100, string sort = null);

        #endregion

        #region Test case attachment controller

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

        #region Test case scenario controller

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

        #region Test tag controller

        /// <summary>
        /// Find all test tags
        /// </summary>
        /// <returns></returns>
        [Get("/api/rs/tag")]
        Task<IApiResponse<List<Tag>>> GetTagsAsync();

        /// <summary>
        /// Create a new test tag
        /// </summary>
        /// <returns></returns>
        [Post("/api/rs/tag")]
        Task<IApiResponse<Tag>> CreateTagAsync([Body] Tag tag);

        #endregion

        #region Custom field schema controller

        /// <summary>
        /// Find all custom field schemas for given project
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="page">Zero-based page index (0..N)</param>
        /// <param name="size">The size of the page to be returned</param>
        /// <param name="sort">Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported.</param>
        /// <returns></returns>
        [Get("/api/rs/cfschema")]
        Task<IApiResponse<GetContentResponse<CustomFieldSchemaContent>>> GetCustomFieldSchemaAsync(int projectId, int page = 0, int size = 100,
            string sort = null);

        #endregion

        #region Custom field value controller

        /// <summary>
        /// Find all custom field values
        /// </summary>
        /// <param name="customFieldId"></param>
        /// <param name="page">Zero-based page index (0..N)</param>
        /// <param name="size">The size of the page to be returned</param>
        /// <param name="sort">Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported.</param>
        /// <returns></returns>
        [Get("/api/rs/cfv")]
        Task<IApiResponse<GetContentResponse<CustomFieldItem>>> GetCustomFieldValuesAsync(long customFieldId, int page = 0, int size = 100,
            string sort = null);

        /// <summary>
        /// Create a new custom field value
        /// </summary>
        [Post("/api/rs/cfv")]
        Task<IApiResponse<CustomFieldItem>> CreateCustomFieldValueAsync([Body] CustomFieldItem customFieldItem);

        #endregion

        #region Test layer schema controller

        /// <summary>
        /// Find all test layer schemas for given project
        /// </summary>
        /// <param name="projectId"></param>
        /// <param name="page">Zero-based page index (0..N)</param>
        /// <param name="size">The size of the page to be returned</param>
        /// <param name="sort">Sorting criteria in the format: property(,asc|desc). Default sort order is ascending. Multiple sort criteria are supported.</param>
        /// <returns></returns>
        [Get("/api/rs/testlayerschema")]
        Task<IApiResponse<GetContentResponse<TestLayerSchemaContent>>> GetTestLayerSchemaAsync(int projectId, int page = 0, int size = 100,
            string sort = null);

        #endregion

        #region Test case bulk controller

        /// <summary>
        /// Remove test cases by ids
        /// </summary>
        [Post("/api/rs/testcase/bulk/remove")]
        Task<IApiResponse<TestCase>> RemoveTestCasesAsync([Body] TestCaseBulk testCaseBulk);

        #endregion

        #region Test case search controller

        /// <summary>
        /// Find all test cases by given AQL
        /// </summary>
        [Get("/api/rs/testcase/__search")]
        Task<IApiResponse<GetContentResponse<TestCase>>> SearchTestCasesAsync(int projectId, string rql, bool deleted = false, int page = 0,
            int size = 100,
            string sort = null);

        #endregion
    }
}