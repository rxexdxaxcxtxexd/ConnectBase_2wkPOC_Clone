# Lyntias Quotation API Documentation

## Overview

This API provides integration with Lyntia's quotation system for telecom connectivity services. It enables automated quotation generation for capacity and internet services across Spain.

## Base URL

- **Production**: `https://apimanager.lyntia.com/api`
- **Pre-Production**: `https://pre-apimanager.lyntia.com/api`

## Authentication

All API requests require Bearer token authentication:

```http
Authorization: Bearer {API_KEY}
```

## Endpoints

### 1. Get Available Carriers

Retrieve list of available carrier locations for quotation.

**Endpoint**: `GET /api/lyntias/carriers`

**Response**: `200 OK`
```json
[
  {
    "cod_prov": 99,
    "codigo_uso": "cab9a0a7-5d77-e911-a84f-000d3a2a78db",
    "municipio": "New York Condado",
    "nombre": "60 Hudson New York"
  },
  {
    "cod_prov": 8,
    "codigo_uso": "cbb9a0a7-5d77-e911-a84f-000d3a2a78db",
    "municipio": "Barcelona",
    "nombre": "Acens Barcelona. Tarragona 161"
  }
]
```

### 2. Create Quotation

Generate a quotation for connectivity service.

**Endpoint**: `POST /api/lyntias/quote`

**Request Body**:
```json
{
  "address": "Gran Vía 39, Madrid",
  "client": "lyntia",
  "service": "capacidad",
  "carrier": "ecc6a6a8-5d77-e911-a84c-000d3a2a711c",
  "capacityMbps": 100,
  "termMonths": 36,
  "offNetOLO": true,
  "CIDR": 30,
  "requestID": "XX-132456"
}
```

**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| address | string | Yes | Requested service address |
| client | string | Yes | Client ID (API Manager username) |
| service | string | Yes | Service type: "capacidad" or "internet" |
| carrier | string | Yes | Carrier GUID from carriers endpoint |
| capacityMbps | integer | No | Speed in Mbps (see available capacities) |
| termMonths | integer | No | Contract term: 12, 24, 36, 48, or 60 months |
| offNetOLO | boolean | No | Allow off-net OLO services |
| CIDR | integer | No | Subnet mask: 28, 29, or 30 |
| requestID | string | No | Buyer's request identifier |

**Response**: `200 OK`
```json
{
  "endA": "Calle Gran Vía, 39, Madrid",
  "coords": {
    "lat": 40.420083,
    "lon": -3.705398,
    "SRID": 4326
  },
  "endB": "Albasanz, 71, 28037 Madrid",
  "capacityMbps": 100,
  "termMonths": 36,
  "nrc": 427,
  "mrc": 427,
  "leadTime": {
    "number": 12,
    "unit": "d"
  },
  "service": "capacidad",
  "CIDR": 30,
  "viability": true,
  "country": 724,
  "currency": "euro",
  "provider": "lyntia",
  "lastMileProv": "Telefónica",
  "channel": "fibra",
  "offerType": "budgetary",
  "offerCode": "OFE-XXXXXX",
  "registryDate": "04/02/2020",
  "offerValidity": {
    "number": 3,
    "unit": "m"
  },
  "notes": "VAT not included in the amounts indicated",
  "requestID": "XX-132456"
}
```

### 3. Get Quotation Details

Retrieve existing quotation by offer code.

**Endpoint**: `GET /api/lyntias/quote/{offerCode}`

**Response**: Returns same structure as Create Quotation response.

## Error Handling

### HTTP Status Codes

| Status | Description |
|--------|-------------|
| 200 | Success |
| 206 | Partial Content - Request processed with warnings |
| 400 | Bad Request - Invalid input data |
| 401 | Unauthorized - Invalid authentication |
| 403 | Forbidden - Invalid token |
| 404 | Not Found - Service unavailable |
| 500 | Internal Server Error |

### Error Response Format

```json
{
  "status": 206,
  "error_code": "AQS001",
  "error_msg": "Error within the input Data"
}
```

### Common Error Codes

| Code | Message | Description |
|------|---------|-------------|
| AQS001 | Error within the input Data | Invalid request parameters |
| AQS002 | Error in Google Geocoding response | Address validation failed |
| AQS003 | Unclear Input address | No geocoding results |
| AQS004 | Address out of Spain | Service only available in Spain |
| AQS005-010 | Autoquotation System Internal Error | System processing errors |
| PR001-006 | Various internal errors | Processing failures |

## Available Capacities (Mbps)

- **Sub-100 Mbps**: 1, 2, 3, 5, 10, 20, 30, 40, 50, 60, 70, 80, 90
- **100-999 Mbps**: 100, 200, 300, 400, 500, 600, 700, 800, 900
- **1-10 Gbps**: 1000, 2000, 2500, 3000, 4000, 5000, 6000, 7000, 8000, 9000, 10000
- **10+ Gbps**: 20000, 30000, 40000, 50000, 100000

## Contract Terms

- 12 months
- 24 months
- 36 months
- 48 months
- 60 months

## CIDR Options

| CIDR | IPs Available | Description |
|------|---------------|-------------|
| 28 | 14 usable IPs | /28 subnet |
| 29 | 6 usable IPs | /29 subnet |
| 30 | 2 usable IPs | /30 subnet (point-to-point) |

## Integration Notes

1. **Address Validation**: All addresses are validated using Google Geocoding API
2. **Service Area**: Currently limited to Spain (country code 724)
3. **Capacity Matching**: System finds nearest available capacity if exact match not available
4. **Lead Times**: Provided in days (d) or months (m)
5. **Currency**: All prices in EUR, VAT not included
6. **Offer Validity**: Typically 3 months from generation date

## Configuration (appsettings.json)

```json
{
  "Lyntias": {
    "ApiBaseUrl": "https://pre-apimanager.lyntia.com/api",
    "ApiKey": "your-api-key-here"
  }
}
```

## Dependencies

- ASP.NET Core 6.0+
- Newtonsoft.Json
- Microsoft.Extensions.Http

## Usage Example

```csharp
// Startup.cs or Program.cs
services.AddHttpClient();
services.AddScoped<LyntiasQuotationController>();

// In your service
var request = new QuotationRequest
{
    Address = "Gran Vía 39, Madrid",
    Client = "your-client-id",
    Service = "capacidad",
    Carrier = "ecc6a6a8-5d77-e911-a84c-000d3a2a711c",
    CapacityMbps = 100,
    TermMonths = 36,
    OffNetOLO = true,
    RequestID = "REQ-2024-001"
};

var response = await _lyntiasController.CreateQuote(request);
```