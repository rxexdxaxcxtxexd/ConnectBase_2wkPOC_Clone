using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TelecomApiAnalyzer.Web.Models
{
    /// <summary>
    /// Portal authentication result model
    /// </summary>
    public class OptusPortalAuthResult
    {
        public bool Success { get; set; }
        public string? SessionId { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
        public string? RedirectUrl { get; set; }
        public DateTime AuthenticatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Portal page retrieval result
    /// </summary>
    public class OptusPortalPageResult
    {
        public bool Success { get; set; }
        public string? HtmlContent { get; set; }
        public string? ErrorMessage { get; set; }
        public bool HasLoginForm { get; set; }
        public string? CsrfToken { get; set; }
        public int StatusCode { get; set; }
    }

    /// <summary>
    /// Portal health status model
    /// </summary>
    public class OptusPortalHealthStatus
    {
        public bool IsPortalAccessible { get; set; }
        public bool IsAuthenticated { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public int StatusCode { get; set; }
        public DateTime LastChecked { get; set; }
        public string? PortalVersion { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Carrier information as parsed from portal HTML
    /// Maps to the generated CarrierInfo model from the document
    /// </summary>
    public class OptusCarrierInfo
    {
        [JsonPropertyName("cod_prov")]
        public int Cod_prov { get; set; }

        [JsonPropertyName("codigo_uso")]
        public string Codigo_uso { get; set; } = string.Empty;

        [JsonPropertyName("municipio")]
        public string Municipio { get; set; } = string.Empty;

        [JsonPropertyName("nombre")]
        public string Nombre { get; set; } = string.Empty;

        // Additional portal-specific properties
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = "Portal HTML";
        public bool IsActive { get; set; } = true;
    }

    /// <summary>
    /// Portal-based quotation request
    /// Maps to the generated QuotationRequest model but adapted for portal forms
    /// </summary>
    public class OptusPortalQuoteRequest
    {
        [Required]
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [Required]
        [JsonPropertyName("client")]
        public string Client { get; set; } = "B2BNitel";

        [Required]
        [JsonPropertyName("service")]
        public string Service { get; set; } = "NBN";

        [Required]
        [JsonPropertyName("carrier")]
        public string Carrier { get; set; } = string.Empty;

        [JsonPropertyName("capacityMbps")]
        public int? CapacityMbps { get; set; }

        [JsonPropertyName("termMonths")]
        public int? TermMonths { get; set; }

        [JsonPropertyName("offNetOLO")]
        public bool? OffNetOLO { get; set; }

        [JsonPropertyName("cIDR")]
        public int? CIDR { get; set; }

        [Required]
        [JsonPropertyName("requestID")]
        public string RequestID { get; set; } = string.Empty;

        // Portal-specific properties
        public string? SessionId { get; set; }
        public string? CsrfToken { get; set; }
        public Dictionary<string, string> AdditionalFormFields { get; set; } = new();
    }

    /// <summary>
    /// Portal quotation result
    /// Represents the response from portal form submission
    /// </summary>
    public class OptusPortalQuoteResult
    {
        public bool Success { get; set; }
        public string? QuoteId { get; set; }
        public string? HtmlResponse { get; set; }
        public int StatusCode { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Parsed response data (if available)
        public OptusPortalQuoteData? ParsedData { get; set; }
    }

    /// <summary>
    /// Parsed quotation data from portal HTML response
    /// Maps to the generated QuotationResponse model structure
    /// </summary>
    public class OptusPortalQuoteData
    {
        [JsonPropertyName("endA")]
        public string EndA { get; set; } = string.Empty;

        [JsonPropertyName("coords")]
        public OptusPortalCoordinates? Coords { get; set; }

        [JsonPropertyName("endB")]
        public string EndB { get; set; } = string.Empty;

        [JsonPropertyName("capacityMbps")]
        public int CapacityMbps { get; set; }

        [JsonPropertyName("termMonths")]
        public int TermMonths { get; set; }

        [JsonPropertyName("nrc")]
        public decimal Nrc { get; set; }

        [JsonPropertyName("mrc")]
        public decimal Mrc { get; set; }

        [JsonPropertyName("leadTime")]
        public OptusPortalLeadTime? LeadTime { get; set; }

        [JsonPropertyName("service")]
        public string Service { get; set; } = string.Empty;

        [JsonPropertyName("offerCode")]
        public string OfferCode { get; set; } = string.Empty;

        // Portal-specific properties
        public string Source { get; set; } = "Portal Form";
        public bool IsEstimate { get; set; } = true;
        public DateTime ValidUntil { get; set; }
    }

    /// <summary>
    /// Coordinates object for portal responses
    /// </summary>
    public class OptusPortalCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string GeocodeSource { get; set; } = "Portal";
    }

    /// <summary>
    /// Lead time information from portal
    /// </summary>
    public class OptusPortalLeadTime
    {
        public int Days { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Portal session management model
    /// </summary>
    public class OptusPortalSession
    {
        public string SessionId { get; set; } = string.Empty;
        public string? CsrfToken { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(30);
        public bool IsAuthenticated { get; set; }
        public string? Username { get; set; }
        public Dictionary<string, string> SessionData { get; set; } = new();

        public bool IsExpired => DateTime.UtcNow - LastActivity > Timeout;

        public void RefreshActivity()
        {
            LastActivity = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Portal form field discovery model
    /// </summary>
    public class OptusPortalFormField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public bool Required { get; set; }
        public string Label { get; set; } = string.Empty;
        public string Placeholder { get; set; } = string.Empty;
        public List<OptusPortalFormOption> Options { get; set; } = new();
        public Dictionary<string, string> Attributes { get; set; } = new();
    }

    /// <summary>
    /// Portal form option for select/dropdown fields
    /// </summary>
    public class OptusPortalFormOption
    {
        public string Value { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public bool Selected { get; set; }
    }

    /// <summary>
    /// Portal endpoint discovery result
    /// </summary>
    public class OptusPortalEndpoint
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public string Description { get; set; } = string.Empty;
        public List<OptusPortalFormField> RequiredFields { get; set; } = new();
        public bool RequiresAuthentication { get; set; }
        public DateTime DiscoveredAt { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = "Portal Analysis";
    }

    /// <summary>
    /// Portal workflow step model
    /// </summary>
    public class OptusPortalWorkflowStep
    {
        public string Name { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Order { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCurrentStep { get; set; }
        public List<OptusPortalFormField> RequiredData { get; set; } = new();
        public Dictionary<string, object> StepData { get; set; } = new();
    }

    /// <summary>
    /// Complete portal analysis result
    /// </summary>
    public class OptusPortalAnalysisResult
    {
        public bool Success { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public List<OptusPortalEndpoint> DiscoveredEndpoints { get; set; } = new();
        public List<OptusPortalFormField> DiscoveredForms { get; set; } = new();
        public List<OptusPortalWorkflowStep> WorkflowSteps { get; set; } = new();
        public List<OptusCarrierInfo> AvailableCarriers { get; set; } = new();
        public string? PortalVersion { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan AnalysisDuration { get; set; }
    }

    /// <summary>
    /// Portal integration test result
    /// </summary>
    public class OptusPortalTestResult
    {
        public string TestName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public TimeSpan Duration { get; set; }
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object> TestData { get; set; } = new();
        public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
        public string Category { get; set; } = "Portal Integration";
    }

    /// <summary>
    /// Portal configuration settings
    /// </summary>
    public class OptusPortalSettings
    {
        public string BaseUrl { get; set; } = "https://optuswholesale.cpq.cloud.sap";
        public string LoginPath { get; set; } = "/Login.aspx";
        public string DashboardPath { get; set; } = "/default.aspx";
        public string QuoteCreatePath { get; set; } = "/Quote/Create";
        public string QuoteSubmitPath { get; set; } = "/Quote/Submit";
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan SessionTimeout { get; set; } = TimeSpan.FromMinutes(30);
        public int MaxRetryAttempts { get; set; } = 3;
        public bool EnableDetailedLogging { get; set; } = true;
        public bool ValidateSSL { get; set; } = true;
    }
}