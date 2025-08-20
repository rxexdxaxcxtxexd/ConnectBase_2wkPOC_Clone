using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TelecomApiAnalyzer.Web.Models
{
    public class OptusApiSettings
    {
        public string BaseUrl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string B2BSQEndpoint { get; set; }
        public string B2BQuoteEndpoint { get; set; }
        public string ContentType { get; set; }
    }

    public class OptusB2BSQRequest
    {
        [Required]
        public string Param { get; set; }
    }

    public class OptusB2BSQResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public OptusB2BSQData Data { get; set; }
        public string RequestId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OptusB2BSQData
    {
        public string ServiceQualificationId { get; set; }
        public string ServiceabilityStatus { get; set; }
        public List<OptusServiceLocation> ServiceLocations { get; set; }
        public List<OptusProductOffering> ProductOfferings { get; set; }
        public OptusGeographicAddress GeographicAddress { get; set; }
    }

    public class OptusB2BQuoteRequest
    {
        [Required]
        public string Param { get; set; }
    }

    public class OptusB2BQuoteResponse
    {
        public string Status { get; set; }
        public string Message { get; set; }
        public OptusB2BQuoteData Data { get; set; }
        public string QuoteId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OptusB2BQuoteData
    {
        public string QuoteNumber { get; set; }
        public string QuoteStatus { get; set; }
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; }
        public DateTime ValidUntil { get; set; }
        public List<OptusQuoteLineItem> LineItems { get; set; }
        public OptusCustomerInfo Customer { get; set; }
        public OptusServiceDetails ServiceDetails { get; set; }
    }

    public class OptusServiceLocation
    {
        public string LocationId { get; set; }
        public string LocationName { get; set; }
        public OptusGeographicAddress Address { get; set; }
        public string ServiceabilityType { get; set; }
        public List<string> AvailableServices { get; set; }
    }

    public class OptusProductOffering
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCategory { get; set; }
        public string ServiceType { get; set; }
        public OptusProductSpecification Specification { get; set; }
        public OptusProductPrice Price { get; set; }
    }

    public class OptusProductSpecification
    {
        public string Bandwidth { get; set; }
        public string Technology { get; set; }
        public string ServiceLevel { get; set; }
        public List<string> Features { get; set; }
    }

    public class OptusProductPrice
    {
        public decimal SetupFee { get; set; }
        public decimal MonthlyRecurringCharge { get; set; }
        public string Currency { get; set; }
        public string BillingCycle { get; set; }
    }

    public class OptusGeographicAddress
    {
        public string StreetNumber { get; set; }
        public string StreetName { get; set; }
        public string StreetType { get; set; }
        public string Suburb { get; set; }
        public string State { get; set; }
        public string PostCode { get; set; }
        public string Country { get; set; }
        public OptusGeoCoordinates Coordinates { get; set; }
    }

    public class OptusGeoCoordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class OptusQuoteLineItem
    {
        public string ItemId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
        public string Description { get; set; }
        public OptusServiceSpecification ServiceSpec { get; set; }
    }

    public class OptusCustomerInfo
    {
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerType { get; set; }
        public OptusGeographicAddress BillingAddress { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
    }

    public class OptusServiceDetails
    {
        public string ServiceType { get; set; }
        public DateTime RequestedDeliveryDate { get; set; }
        public string ContractTerm { get; set; }
        public List<OptusServiceCharacteristic> ServiceCharacteristics { get; set; }
        public OptusGeographicAddress ServiceAddress { get; set; }
    }

    public class OptusServiceSpecification
    {
        public string Bandwidth { get; set; }
        public string ServiceLevel { get; set; }
        public string ContractLength { get; set; }
        public List<string> AdditionalFeatures { get; set; }
    }

    public class OptusServiceCharacteristic
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public string ValueType { get; set; }
    }

    public class OptusApiError
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorDescription { get; set; }
        public List<OptusFieldError> FieldErrors { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class OptusFieldError
    {
        public string FieldName { get; set; }
        public string ErrorMessage { get; set; }
        public string InvalidValue { get; set; }
    }

    // Request parameter models for form-encoded data
    public class OptusB2BSQParams
    {
        public string ServiceAddress { get; set; }
        public string PostCode { get; set; }
        public string State { get; set; }
        public string ServiceType { get; set; }
        public string Bandwidth { get; set; }
        public string CustomerId { get; set; }
        public string RequestId { get; set; }
    }

    public class OptusB2BQuoteParams
    {
        public string ServiceQualificationId { get; set; }
        public string ProductId { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string ServiceAddress { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public string RequestedDeliveryDate { get; set; }
        public string ContractTerm { get; set; }
        public string RequestId { get; set; }
    }
}