using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TelecomApiAnalyzer.Web.Models
{
    public class ApiDocument
    {
        public Guid Id { get; set; }
        public string FileName { get; set; }
        public string Content { get; set; }
        public DateTime UploadedAt { get; set; }
        public string ProcessingStatus { get; set; }
        public TechnicalSpecification TechnicalSpec { get; set; }
        public UseCaseGuide UseCaseGuide { get; set; }
    }

    public class TechnicalSpecification
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Version { get; set; }
        public string Content { get; set; }
        public List<ApiEndpoint> Endpoints { get; set; }
        public List<DataModel> Models { get; set; }
        public AuthenticationDetails Authentication { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class UseCaseGuide
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public List<UseCase> UseCases { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class ApiEndpoint
    {
        public string Name { get; set; }
        public string Method { get; set; }
        public string Path { get; set; }
        public string Description { get; set; }
        public List<Parameter> Parameters { get; set; }
        public ResponseModel Response { get; set; }
    }

    public class Parameter
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Description { get; set; }
        public string Example { get; set; }
    }

    public class ResponseModel
    {
        public string ContentType { get; set; }
        public string Schema { get; set; }
        public string Example { get; set; }
    }

    public class DataModel
    {
        public string Name { get; set; }
        public List<Property> Properties { get; set; }
    }

    public class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool Required { get; set; }
        public string Description { get; set; }
    }

    public class UseCase
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Scenario { get; set; }
        public string ExpectedResult { get; set; }
        public string ErrorHandling { get; set; }
    }

    public class AuthenticationDetails
    {
        public string Type { get; set; }
        public string TokenEndpoint { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public Dictionary<string, string> AdditionalParameters { get; set; }
    }

    public class WorkflowStep
    {
        public int StepNumber { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? CompletedAt { get; set; }
    }

    public class ApiAnalysisProject
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public ApiDocument Document { get; set; }
        public List<WorkflowStep> WorkflowSteps { get; set; }
        public GeneratedCode Code { get; set; }
        public PostmanCollection PostmanCollection { get; set; }
        public BicepTemplate DeploymentTemplate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastModifiedAt { get; set; }
    }

    public class GeneratedCode
    {
        public Guid Id { get; set; }
        public string Language { get; set; }
        public Dictionary<string, string> Files { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class PostmanCollection
    {
        public string Name { get; set; }
        public string JsonContent { get; set; }
        public DateTime GeneratedAt { get; set; }
    }

    public class BicepTemplate
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public string ParametersFile { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}