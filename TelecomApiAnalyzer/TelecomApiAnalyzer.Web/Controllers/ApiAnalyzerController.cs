using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TelecomApiAnalyzer.Web.Models;
using TelecomApiAnalyzer.Web.Services;

namespace TelecomApiAnalyzer.Web.Controllers
{
    public class ApiAnalyzerController : Controller
    {
        private readonly ILogger<ApiAnalyzerController> _logger;
        private readonly IApiDocumentAnalyzer _documentAnalyzer;
        private readonly ICodeGenerationService _codeGenerator;
        private readonly IPostmanCollectionGenerator _postmanGenerator;
        private readonly ITestRunnerService _testRunnerService;
        private static readonly Dictionary<Guid, ApiAnalysisProject> _projects = new();

        public ApiAnalyzerController(
            ILogger<ApiAnalyzerController> logger,
            IApiDocumentAnalyzer documentAnalyzer,
            ICodeGenerationService codeGenerator,
            IPostmanCollectionGenerator postmanGenerator,
            ITestRunnerService testRunnerService)
        {
            _logger = logger;
            _documentAnalyzer = documentAnalyzer;
            _codeGenerator = codeGenerator;
            _postmanGenerator = postmanGenerator;
            _testRunnerService = testRunnerService;
        }

        public IActionResult Index()
        {
            var model = new ApiAnalyzerViewModel
            {
                Projects = _projects.Values.OrderByDescending(p => p.CreatedAt).ToList()
            };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UploadDocument(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Please select a file to upload.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var projectId = Guid.NewGuid();
                var document = new ApiDocument
                {
                    Id = Guid.NewGuid(),
                    FileName = file.FileName,
                    UploadedAt = DateTime.UtcNow,
                    ProcessingStatus = "Uploaded"
                };

                using (var reader = new StreamReader(file.OpenReadStream()))
                {
                    document.Content = await reader.ReadToEndAsync();
                }

                var project = new ApiAnalysisProject
                {
                    Id = projectId,
                    Name = Path.GetFileNameWithoutExtension(file.FileName),
                    Document = document,
                    CreatedAt = DateTime.UtcNow,
                    WorkflowSteps = InitializeWorkflowSteps()
                };

                _projects[projectId] = project;
                
                return RedirectToAction(nameof(AnalyzeDocument), new { projectId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                TempData["Error"] = "An error occurred while uploading the document.";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> AnalyzeDocument(Guid projectId)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return NotFound();
            }

            try
            {
                UpdateWorkflowStep(project, 1, "In Progress");
                
                var technicalSpec = await _documentAnalyzer.AnalyzeDocumentAsync(project.Document);
                var useCaseGuide = await _documentAnalyzer.GenerateUseCaseGuideAsync(project.Document);
                
                project.Document.TechnicalSpec = technicalSpec;
                project.Document.UseCaseGuide = useCaseGuide;
                project.Document.ProcessingStatus = "Analyzed";
                
                UpdateWorkflowStep(project, 1, "Completed");
                project.LastModifiedAt = DateTime.UtcNow;
                
                return View("Analysis", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing document");
                UpdateWorkflowStep(project, 1, "Failed");
                TempData["Error"] = "An error occurred while analyzing the document.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSpecification(Guid projectId, string technicalSpec, string useCaseGuide)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return NotFound();
            }

            project.Document.TechnicalSpec.Content = technicalSpec;
            project.Document.UseCaseGuide.Content = useCaseGuide;
            project.LastModifiedAt = DateTime.UtcNow;

            TempData["Success"] = "Specifications updated successfully.";
            return RedirectToAction(nameof(GenerateCode), new { projectId });
        }

        public async Task<IActionResult> GenerateCode(Guid projectId)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return NotFound();
            }

            try
            {
                UpdateWorkflowStep(project, 2, "In Progress");
                
                var generatedCode = await _codeGenerator.GenerateCodeAsync(project.Document.TechnicalSpec);
                project.Code = generatedCode;
                
                UpdateWorkflowStep(project, 2, "Completed");
                project.LastModifiedAt = DateTime.UtcNow;
                
                return View("GeneratedCode", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating code");
                UpdateWorkflowStep(project, 2, "Failed");
                TempData["Error"] = "An error occurred while generating code.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult Authentication(Guid projectId)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return NotFound();
            }

            try
            {
                UpdateWorkflowStep(project, 3, "In Progress");
                
                // Initialize authentication with OPTUS production settings if not already set
                if (project.Document.TechnicalSpec.Authentication == null)
                {
                    project.Document.TechnicalSpec.Authentication = new AuthenticationDetails
                    {
                        Type = "Basic",
                        ClientId = "B2BNitel",
                        ClientSecret = "Shetry!$990",
                        TokenEndpoint = "https://optuswholesale.cpq.cloud.sap/oauth/token"
                    };
                }
                
                project.LastModifiedAt = DateTime.UtcNow;
                return View("Authentication", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading authentication configuration");
                UpdateWorkflowStep(project, 3, "Failed");
                TempData["Error"] = "An error occurred while loading authentication configuration.";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public IActionResult SaveAuthentication(Guid projectId, string authType, string clientId, string clientSecret, string tokenEndpoint)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return NotFound();
            }

            project.Document.TechnicalSpec.Authentication = new AuthenticationDetails
            {
                Type = authType,
                ClientId = clientId,
                ClientSecret = clientSecret,
                TokenEndpoint = tokenEndpoint
            };

            UpdateWorkflowStep(project, 3, "Completed");
            project.LastModifiedAt = DateTime.UtcNow;

            TempData["Success"] = "Authentication details saved successfully.";
            return RedirectToAction(nameof(TestConfiguration), new { projectId });
        }

        public async Task<IActionResult> TestConfiguration(Guid projectId)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return NotFound();
            }

            // Ensure authentication is completed before proceeding to testing
            var authStep = project.WorkflowSteps.FirstOrDefault(s => s.StepNumber == 3);
            if (authStep?.Status != "Completed")
            {
                TempData["Error"] = "Please complete authentication configuration before proceeding to testing.";
                return RedirectToAction(nameof(Authentication), new { projectId });
            }

            try
            {
                UpdateWorkflowStep(project, 5, "In Progress");
                
                // Generate Postman collection for testing
                var postmanCollection = await _postmanGenerator.GenerateCollectionAsync(project.Document.TechnicalSpec);
                project.PostmanCollection = postmanCollection;
                
                UpdateWorkflowStep(project, 5, "Completed");
                project.LastModifiedAt = DateTime.UtcNow;
                
                return View("TestConfiguration", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating test configuration");
                UpdateWorkflowStep(project, 5, "Failed");
                TempData["Error"] = "An error occurred while generating test configuration.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult DownloadPostmanCollection(Guid projectId)
        {
            if (!_projects.TryGetValue(projectId, out var project) || project.PostmanCollection == null)
            {
                return NotFound();
            }

            var bytes = Encoding.UTF8.GetBytes(project.PostmanCollection.JsonContent);
            return File(bytes, "application/json", $"{project.PostmanCollection.Name}.postman_collection.json");
        }

        public async Task<IActionResult> GenerateDeployment(Guid projectId)
        {
            if (!_projects.TryGetValue(projectId, out var project))
            {
                return NotFound();
            }

            try
            {
                UpdateWorkflowStep(project, 6, "In Progress");
                
                var bicepTemplate = await GenerateBicepTemplate(project);
                project.DeploymentTemplate = bicepTemplate;
                
                UpdateWorkflowStep(project, 6, "Completed");
                project.LastModifiedAt = DateTime.UtcNow;
                
                return View("Deployment", project);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating deployment template");
                UpdateWorkflowStep(project, 6, "Failed");
                TempData["Error"] = "An error occurred while generating deployment template.";
                return RedirectToAction(nameof(Index));
            }
        }

        public IActionResult DownloadBicepTemplate(Guid projectId)
        {
            if (!_projects.TryGetValue(projectId, out var project) || project.DeploymentTemplate == null)
            {
                return NotFound();
            }

            var bytes = Encoding.UTF8.GetBytes(project.DeploymentTemplate.Content);
            return File(bytes, "text/plain", $"{project.DeploymentTemplate.Name}.bicep");
        }

        private List<WorkflowStep> InitializeWorkflowSteps()
        {
            return new List<WorkflowStep>
            {
                new WorkflowStep { StepNumber = 0, Name = "Upload API Document", Description = "Upload API documentation file", Status = "Completed", CompletedAt = DateTime.UtcNow },
                new WorkflowStep { StepNumber = 1, Name = "Analyze Document", Description = "Extract API specifications", Status = "Pending" },
                new WorkflowStep { StepNumber = 2, Name = "Generate Code", Description = "Generate API client code", Status = "Pending" },
                new WorkflowStep { StepNumber = 3, Name = "Authentication Details", Description = "Configure authentication", Status = "Pending" },
                new WorkflowStep { StepNumber = 4, Name = "Customer Mapping", Description = "Map customer fields", Status = "Pending" },
                new WorkflowStep { StepNumber = 5, Name = "Run Tests", Description = "Generate and run tests", Status = "Pending" },
                new WorkflowStep { StepNumber = 6, Name = "Deploy", Description = "Deploy to Azure", Status = "Pending" }
            };
        }

        private void UpdateWorkflowStep(ApiAnalysisProject project, int stepNumber, string status)
        {
            var step = project.WorkflowSteps.FirstOrDefault(s => s.StepNumber == stepNumber);
            if (step != null)
            {
                step.Status = status;
                if (status == "Completed")
                {
                    step.CompletedAt = DateTime.UtcNow;
                }
            }
        }

        private async Task<BicepTemplate> GenerateBicepTemplate(ApiAnalysisProject project)
        {
            var template = new BicepTemplate
            {
                Name = $"deploy-{project.Name.ToLower().Replace(" ", "-")}",
                GeneratedAt = DateTime.UtcNow
            };

            template.Content = await Task.FromResult(GenerateBicepContent(project));
            template.ParametersFile = GenerateBicepParameters(project);

            return template;
        }

        private string GenerateBicepContent(ApiAnalysisProject project)
        {
            return @"@description('Name of the App Service')
param appServiceName string

@description('Location for all resources')
param location string = resourceGroup().location

@description('The SKU of App Service Plan')
param sku string = 'B1'

@description('Runtime stack')
param linuxFxVersion string = 'DOTNETCORE|6.0'

var appServicePlanName = '${appServiceName}-plan'
var applicationInsightsName = '${appServiceName}-insights'

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServicePlanName
  location: location
  sku: {
    name: sku
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource appService 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceName
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      appSettings: [
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: applicationInsights.properties.ConnectionString
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~3'
        }
      ]
    }
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

output appServiceUrl string = 'https://${appService.properties.defaultHostName}'";
        }

        private string GenerateBicepParameters(ApiAnalysisProject project)
        {
            return $@"{{
  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {{
    ""appServiceName"": {{
      ""value"": ""{project.Name.ToLower().Replace(" ", "-")}-api""
    }},
    ""location"": {{
      ""value"": ""eastus""
    }},
    ""sku"": {{
      ""value"": ""B1""
    }}
  }}
}}";
        }

        [HttpPost]
        public async Task<IActionResult> RunTestSuite(Guid id, [FromBody] TestRunRequest request)
        {
            _logger.LogInformation("RunTestSuite called with id: {ProjectId}", id);
            
            if (request == null)
            {
                _logger.LogWarning("Request is null");
                return BadRequest("Request body is required");
            }

            if (!_projects.TryGetValue(id, out var project))
            {
                _logger.LogWarning("Project not found: {ProjectId}", id);
                return NotFound($"Project {id} not found");
            }

            try
            {
                UpdateWorkflowStep(project, 5, "In Progress");

                var testSuite = await _testRunnerService.RunTestSuiteAsync(project, request.Configuration);
                
                UpdateWorkflowStep(project, 5, testSuite.Status == TestStatus.Passed ? "Completed" : "Failed");
                project.LastModifiedAt = DateTime.UtcNow;

                return Json(new { 
                    success = true, 
                    testSuiteId = testSuite.Id,
                    results = testSuite
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error running test suite for project {ProjectId}", id);
                UpdateWorkflowStep(project, 5, "Failed");
                
                return Json(new { 
                    success = false, 
                    error = "An error occurred while running tests: " + ex.Message 
                });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTestSuite(Guid testSuiteId)
        {
            var testSuite = await _testRunnerService.GetTestSuiteAsync(testSuiteId);
            if (testSuite == null)
            {
                return NotFound();
            }

            return Json(testSuite);
        }

        [HttpGet]
        public async Task<IActionResult> GetTestProgress(Guid testSuiteId)
        {
            var testSuite = await _testRunnerService.GetTestSuiteAsync(testSuiteId);
            if (testSuite == null)
            {
                return NotFound();
            }

            var progress = new TestRunProgress
            {
                TestSuiteId = testSuite.Id,
                TotalTests = testSuite.TotalCount,
                CompletedTests = testSuite.TestCases.Count(tc => tc.Status == TestStatus.Passed || tc.Status == TestStatus.Failed),
                PassedTests = testSuite.PassedCount,
                FailedTests = testSuite.FailedCount,
                Status = testSuite.Status,
                CurrentTest = testSuite.TestCases.FirstOrDefault(tc => tc.Status == TestStatus.Running)?.Name ?? ""
            };

            return Json(progress);
        }

        [HttpPost]
        public async Task<IActionResult> ValidateApiEndpoint([FromBody] TestConfiguration configuration)
        {
            try
            {
                var isValid = await _testRunnerService.ValidateEndpointAsync(
                    configuration.BaseUrl, 
                    "/api/carriers", 
                    configuration);

                return Json(new { success = true, isValid });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating API endpoint");
                return Json(new { success = false, error = ex.Message });
            }
        }
    }

    public class ApiAnalyzerViewModel
    {
        public List<ApiAnalysisProject> Projects { get; set; } = new();
    }
}