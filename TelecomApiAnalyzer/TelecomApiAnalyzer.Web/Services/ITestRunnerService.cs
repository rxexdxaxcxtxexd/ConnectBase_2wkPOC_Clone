using System;
using System.Threading;
using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;

namespace TelecomApiAnalyzer.Web.Services
{
    public interface ITestRunnerService
    {
        /// <summary>
        /// Runs a complete test suite for the given project
        /// </summary>
        Task<TestSuite> RunTestSuiteAsync(ApiAnalysisProject project, TestConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Runs a single test case
        /// </summary>
        Task<TestCase> RunTestCaseAsync(TestCase testCase, TestConfiguration configuration, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates test cases for the project's API endpoints
        /// </summary>
        Task<List<TestCase>> GenerateTestCasesAsync(ApiAnalysisProject project);

        /// <summary>
        /// Gets the test suite by ID
        /// </summary>
        Task<TestSuite?> GetTestSuiteAsync(Guid testSuiteId);

        /// <summary>
        /// Gets all test suites for a project
        /// </summary>
        Task<List<TestSuite>> GetTestSuitesAsync(Guid projectId);

        /// <summary>
        /// Validates if the API endpoint is accessible
        /// </summary>
        Task<bool> ValidateEndpointAsync(string baseUrl, string endpoint, TestConfiguration configuration);
    }
}