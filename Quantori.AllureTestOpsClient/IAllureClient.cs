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
        Task<IApiResponse<GetTestCasesResponse>> GetTestCasesAsync(int projectId, int page = 0, int size = 100, string sort = null);

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
        [Get("/api/rs/testcase/{Id}/overview")]
        Task<IApiResponse<TestCaseOverview>> GetTestCaseOverviewAsync([AliasAs("id")] ulong testCaseId);

        /// <summary>
        /// Update a test case
        /// </summary>
        /// <param name="testCaseId"></param>
        /// <param name="updateTestCaseRequest"></param>
        /// <returns></returns>
        [Patch("/api/rs/testcase/{Id}")]
        Task<IApiResponse<TestCase>> UpdateTestCaseAsync([AliasAs("id")] ulong testCaseId, [Body] UpdateTestCaseRequest updateTestCaseRequest);
    }
}