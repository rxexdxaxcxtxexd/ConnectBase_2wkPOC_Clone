using System;
using System.Collections.Generic;

namespace TelecomApiAnalyzer.Web.Models
{
    public class CarrierInfo
    {
        public int CodProv { get; set; }
        public string CodigoUso { get; set; }
        public string Municipio { get; set; }
        public string Nombre { get; set; }
    }

    public class QuotationRequest
    {
        public string Address { get; set; }
        public string Client { get; set; }
        public string Service { get; set; }
        public string Carrier { get; set; }
        public int? CapacityMbps { get; set; }
        public int? TermMonths { get; set; }
        public bool OffNetOLO { get; set; }
        public int? CIDR { get; set; }
        public string RequestID { get; set; }
    }

    public class QuotationResponse
    {
        public string EndA { get; set; }
        public CoordinatesInfo Coords { get; set; }
        public string EndB { get; set; }
        public int CapacityMbps { get; set; }
        public int TermMonths { get; set; }
        public decimal Nrc { get; set; }
        public decimal Mrc { get; set; }
        public LeadTimeInfo LeadTime { get; set; }
        public string Service { get; set; }
        public int CIDR { get; set; }
        public bool Viability { get; set; }
        public int Country { get; set; }
        public string Currency { get; set; }
        public string Provider { get; set; }
        public string LastMileProv { get; set; }
        public string Channel { get; set; }
        public string OfferType { get; set; }
        public string OfferCode { get; set; }
        public string RegistryDate { get; set; }
        public OfferValidityInfo OfferValidity { get; set; }
        public string Notes { get; set; }
    }

    public class CoordinatesInfo
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public int SRID { get; set; }
    }

    public class LeadTimeInfo
    {
        public int Number { get; set; }
        public string Unit { get; set; }
    }

    public class OfferValidityInfo
    {
        public int Number { get; set; }
        public string Unit { get; set; }
    }

    public class ApiError
    {
        public int Status { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMsg { get; set; }
        public string Description { get; set; }
    }

    public class CapacityCatalog
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
    }

    public class TermCatalog
    {
        public string Name { get; set; }
        public string Unit { get; set; }
    }

    public class InternetCIDR
    {
        public string Definition { get; set; }
        public int Units { get; set; }
    }
}