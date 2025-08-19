using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TelecomApiAnalyzer.Web.Models
{
    public class TestSuite
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string BaseUrl { get; set; } = "";
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TestStatus Status { get; set; }
        public List<TestCase> TestCases { get; set; } = new();
        public TimeSpan TotalDuration => CompletedAt.HasValue ? CompletedAt.Value - StartedAt : TimeSpan.Zero;
        public int PassedCount => TestCases.Count(tc => tc.Status == TestStatus.Passed);
        public int FailedCount => TestCases.Count(tc => tc.Status == TestStatus.Failed);
        public int TotalCount => TestCases.Count;
    }

    public class TestCase
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Method { get; set; } = "";
        public string Endpoint { get; set; } = "";
        public string? RequestBody { get; set; }
        public Dictionary<string, string> Headers { get; set; } = new();
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public TestStatus Status { get; set; }
        public string? ResponseBody { get; set; }
        public int? ResponseStatusCode { get; set; }
        public long? ResponseTimeMs { get; set; }
        public string? ErrorMessage { get; set; }
        public List<TestAssertion> Assertions { get; set; } = new();
        public TimeSpan Duration => CompletedAt.HasValue && StartedAt.HasValue ? CompletedAt.Value - StartedAt.Value : TimeSpan.Zero;
    }

    public class TestAssertion
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public bool Passed { get; set; }
        public string? ActualValue { get; set; }
        public string? ExpectedValue { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class TestConfiguration
    {
        public string BaseUrl { get; set; } = "";
        public AuthenticationDetails? Authentication { get; set; }
        public Dictionary<string, string> GlobalHeaders { get; set; } = new();
        public int TimeoutMs { get; set; } = 30000;
        public bool ValidateSchema { get; set; } = true;
        public bool ValidateResponseTime { get; set; } = true;
        public int MaxResponseTimeMs { get; set; } = 5000;
    }

    public class TestRunRequest
    {
        public Guid ProjectId { get; set; }
        public TestConfiguration Configuration { get; set; } = new();
        public List<string>? SelectedEndpoints { get; set; }
    }

    public class TestRunProgress
    {
        public Guid TestSuiteId { get; set; }
        public int TotalTests { get; set; }
        public int CompletedTests { get; set; }
        public int PassedTests { get; set; }
        public int FailedTests { get; set; }
        public string CurrentTest { get; set; } = "";
        public TestStatus Status { get; set; }
        public string? Message { get; set; }
        public int ProgressPercentage => TotalTests > 0 ? (CompletedTests * 100) / TotalTests : 0;
    }

    public enum TestStatus
    {
        Pending,
        Running,
        Passed,
        Failed,
        Skipped,
        Cancelled
    }
}