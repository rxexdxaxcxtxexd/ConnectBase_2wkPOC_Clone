using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ConnectBase.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LyntiasQuotationController : ControllerBase
    {
        private readonly ILogger<LyntiasQuotationController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _apiBaseUrl;
        private readonly string _apiKey;

        public LyntiasQuotationController(
            ILogger<LyntiasQuotationController> logger,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _configuration = configuration;
            _httpClientFactory = httpClientFactory;
            _apiBaseUrl = _configuration["Lyntias:ApiBaseUrl"] ?? "https://pre-apimanager.lyntia.com/api";
            _apiKey = _configuration["Lyntias:ApiKey"] ?? string.Empty;
        }

        [HttpGet("carriers")]
        [ProducesResponseType(typeof(List<CarrierInfo>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCarriers()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                
                var response = await client.GetAsync($"{_apiBaseUrl}/carriers");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var carriers = JsonConvert.DeserializeObject<List<CarrierInfo>>(content);
                    return Ok(carriers);
                }
                
                _logger.LogError($"Failed to retrieve carriers. Status: {response.StatusCode}");
                return StatusCode((int)response.StatusCode, "Failed to retrieve carrier information");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving carriers");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("quote")]
        [ProducesResponseType(typeof(QuotationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status206PartialContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateQuote([FromBody] QuotationRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponse 
                    { 
                        ErrorCode = "AQS001", 
                        ErrorMsg = "Error within the input Data",
                        Status = 400
                    });
                }

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                client.DefaultRequestHeaders.Add("Content-Type", "application/json;charset=UTF-8");
                
                var jsonContent = JsonConvert.SerializeObject(request);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync($"{_apiBaseUrl}/quote", httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    var quotation = JsonConvert.DeserializeObject<QuotationResponse>(responseContent);
                    return Ok(quotation);
                }
                else if (response.StatusCode == HttpStatusCode.PartialContent)
                {
                    var error = JsonConvert.DeserializeObject<ErrorResponse>(responseContent);
                    return StatusCode(206, error);
                }
                else if (response.StatusCode == HttpStatusCode.Forbidden)
                {
                    return Unauthorized(new { message = "Authentication Error. Invalid token" });
                }
                else if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    return NotFound(new { message = "Webservice unavailable or invalid parameters" });
                }
                
                return StatusCode((int)response.StatusCode, responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating quotation");
                return StatusCode(500, new ErrorResponse
                {
                    ErrorCode = "AQS005",
                    ErrorMsg = "Autoquotation System Internal Error",
                    Status = 206
                });
            }
        }

        [HttpGet("quote/{offerCode}")]
        [ProducesResponseType(typeof(QuotationResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetQuote(string offerCode)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
                
                var response = await client.GetAsync($"{_apiBaseUrl}/quote/{offerCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var quotation = JsonConvert.DeserializeObject<QuotationResponse>(content);
                    return Ok(quotation);
                }
                
                return NotFound(new { message = $"Quote {offerCode} not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving quote {offerCode}");
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class CarrierInfo
    {
        [JsonProperty("cod_prov")]
        public int CodProv { get; set; }
        
        [JsonProperty("codigo_uso")]
        public string CodigoUso { get; set; }
        
        [JsonProperty("municipio")]
        public string Municipio { get; set; }
        
        [JsonProperty("nombre")]
        public string Nombre { get; set; }
    }

    public class QuotationRequest
    {
        [JsonProperty("address")]
        [Required]
        public string Address { get; set; }
        
        [JsonProperty("client")]
        [Required]
        public string Client { get; set; }
        
        [JsonProperty("service")]
        [Required]
        public string Service { get; set; }
        
        [JsonProperty("carrier")]
        [Required]
        public string Carrier { get; set; }
        
        [JsonProperty("capacityMbps")]
        public int? CapacityMbps { get; set; }
        
        [JsonProperty("termMonths")]
        public int? TermMonths { get; set; }
        
        [JsonProperty("offNetOLO")]
        public bool OffNetOLO { get; set; }
        
        [JsonProperty("CIDR")]
        public int? CIDR { get; set; }
        
        [JsonProperty("requestID")]
        public string RequestID { get; set; }
    }

    public class QuotationResponse
    {
        [JsonProperty("endA")]
        public string EndA { get; set; }
        
        [JsonProperty("coords")]
        public Coordinates Coords { get; set; }
        
        [JsonProperty("endB")]
        public string EndB { get; set; }
        
        [JsonProperty("capacityMbps")]
        public int CapacityMbps { get; set; }
        
        [JsonProperty("termMonths")]
        public int TermMonths { get; set; }
        
        [JsonProperty("nrc")]
        public decimal Nrc { get; set; }
        
        [JsonProperty("mrc")]
        public decimal Mrc { get; set; }
        
        [JsonProperty("leadTime")]
        public TimeUnit LeadTime { get; set; }
        
        [JsonProperty("service")]
        public string Service { get; set; }
        
        [JsonProperty("CIDR")]
        public int CIDR { get; set; }
        
        [JsonProperty("viability")]
        public bool Viability { get; set; }
        
        [JsonProperty("country")]
        public int Country { get; set; }
        
        [JsonProperty("currency")]
        public string Currency { get; set; }
        
        [JsonProperty("provider")]
        public string Provider { get; set; }
        
        [JsonProperty("lastMileProv")]
        public string LastMileProv { get; set; }
        
        [JsonProperty("channel")]
        public string Channel { get; set; }
        
        [JsonProperty("offerType")]
        public string OfferType { get; set; }
        
        [JsonProperty("offerCode")]
        public string OfferCode { get; set; }
        
        [JsonProperty("registryDate")]
        public string RegistryDate { get; set; }
        
        [JsonProperty("offerValidity")]
        public TimeUnit OfferValidity { get; set; }
        
        [JsonProperty("notes")]
        public string Notes { get; set; }
        
        [JsonProperty("requestID")]
        public string RequestID { get; set; }
    }

    public class Coordinates
    {
        [JsonProperty("lat")]
        public double Lat { get; set; }
        
        [JsonProperty("lon")]
        public double Lon { get; set; }
        
        [JsonProperty("SRID")]
        public int SRID { get; set; }
    }

    public class TimeUnit
    {
        [JsonProperty("number")]
        public int Number { get; set; }
        
        [JsonProperty("unit")]
        public string Unit { get; set; }
    }

    public class ErrorResponse
    {
        [JsonProperty("status")]
        public int Status { get; set; }
        
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }
        
        [JsonProperty("error_msg")]
        public string ErrorMsg { get; set; }
    }

    public static class LyntiasConstants
    {
        public static class Services
        {
            public const string Capacity = "capacidad";
            public const string Internet = "internet";
        }

        public static class Channels
        {
            public const string Fiber = "fibra";
            public const string Radio = "radio";
        }

        public static class OfferTypes
        {
            public const string Budgetary = "budgetary";
        }

        public static readonly int[] AvailableCapacities = new[]
        {
            1, 2, 3, 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100,
            200, 300, 400, 500, 600, 700, 800, 900, 1000, 2000,
            2500, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000,
            20000, 30000, 40000, 50000, 100000
        };

        public static readonly int[] AvailableTerms = new[] { 12, 24, 36, 48, 60 };
        
        public static readonly int[] AvailableCIDR = new[] { 28, 29, 30 };
    }
}