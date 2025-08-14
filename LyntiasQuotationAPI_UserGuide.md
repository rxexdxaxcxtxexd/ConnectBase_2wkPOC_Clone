# Lyntias Quotation API - User Guide & Use Cases

## Overview
This guide walks through practical use cases for the Lyntias Quotation API, demonstrating the complete workflow from carrier selection to quotation generation for telecom connectivity services.

## Workflow Diagram
```
┌─────────────────┐
│ 1. Get Carriers │
└────────┬────────┘
         ↓
┌─────────────────┐
│ 2. Select       │
│    Carrier      │
└────────┬────────┘
         ↓
┌─────────────────┐
│ 3. Submit       │
│    Quote Request│
└────────┬────────┘
         ↓
┌─────────────────┐
│ 4. Receive      │
│    Quotation    │
└─────────────────┘
```

## Use Case Scenarios

### Use Case 0: Successful Quotation Generation ✅

**Scenario**: A customer needs a 100 Mbps fiber connection in Madrid for 36 months.

**Step 1: Retrieve Available Carriers**
```http
GET /api/lyntias/carriers
Authorization: Bearer {YOUR_API_KEY}
```

**Response**:
```json
[
  {
    "cod_prov": 28,
    "codigo_uso": "ccb9a0a7-5d77-e911-a84f-000d3a2a78db",
    "municipio": "Alcobendas",
    "nombre": "Acens Madrid. San Rafael 14"
  }
]
```

**Step 2: Create Quotation Request**
```http
POST /api/lyntias/quote
Authorization: Bearer {YOUR_API_KEY}
Content-Type: application/json

{
  "address": "Gran Vía 39, Madrid",
  "client": "connectbase-user",
  "service": "capacidad",
  "carrier": "ccb9a0a7-5d77-e911-a84f-000d3a2a78db",
  "capacityMbps": 100,
  "termMonths": 36,
  "offNetOLO": true,
  "CIDR": 30,
  "requestID": "CB-2024-0789"
}
```

**Step 3: Receive Successful Quote**
```json
{
  "endA": "Calle Gran Vía, 39, Madrid",
  "coords": {
    "lat": 40.420083,
    "lon": -3.705398,
    "SRID": 4326
  },
  "endB": "San Rafael 14, Alcobendas",
  "capacityMbps": 100,
  "termMonths": 36,
  "nrc": 427,
  "mrc": 427,
  "leadTime": {
    "number": 12,
    "unit": "d"
  },
  "service": "capacidad",
  "offerCode": "OFE-2024-1234",
  "registryDate": "13/08/2025",
  "offerValidity": {
    "number": 3,
    "unit": "m"
  },
  "notes": "VAT not included. Prices subject to final confirmation."
}
```

**Result**: Quote successfully generated with 12-day lead time, €427 setup cost, €427 monthly cost.

---

### Use Case 1: Carrier Not Available ❌

**Scenario**: Attempting to quote without valid carrier information.

**Request**:
```json
{
  "address": "Gran Vía 39, Madrid",
  "client": "connectbase-user",
  "service": "capacidad",
  "carrier": "invalid-carrier-id",
  "capacityMbps": 100,
  "termMonths": 36
}
```

**Response**: `400 Bad Request`
```json
{
  "status": 400,
  "error_code": "AQS001",
  "error_msg": "Error within the input Data - Carrier not available"
}
```

**Resolution**: Fetch valid carriers using GET /carriers endpoint first.

---

### Use Case 2: Web Service Not Available ❌

**Scenario**: Lyntia's backend service is temporarily unavailable.

**Response**: `404 Not Found`
```json
{
  "message": "Webservice unavailable or invalid parameters"
}
```

**Resolution**: Retry after a few minutes or contact support.

---

### Use Case 3: Data Validation Error ❌

**Scenario**: Invalid capacity or term requested.

**Request**:
```json
{
  "address": "Gran Vía 39, Madrid",
  "client": "connectbase-user",
  "service": "capacidad",
  "carrier": "ccb9a0a7-5d77-e911-a84f-000d3a2a78db",
  "capacityMbps": 150,  // Not in available capacities
  "termMonths": 18      // Not a valid term option
}
```

**Response**: `206 Partial Content`
```json
{
  "status": 206,
  "error_code": "AQS001",
  "error_msg": "Error within the input Data - Invalid capacity/term"
}
```

**Valid Options**:
- **Capacities**: 100, 200, 300, 400, 500 Mbps (not 150)
- **Terms**: 12, 24, 36, 48, 60 months (not 18)

---

### Use Case 4: Address Not Found ❌

**Scenario**: Invalid or unclear address that cannot be geocoded.

**Request**:
```json
{
  "address": "Nonexistent Street 999, Madrid",
  "client": "connectbase-user",
  "service": "capacidad",
  "carrier": "ccb9a0a7-5d77-e911-a84f-000d3a2a78db"
}
```

**Response**: `206 Partial Content`
```json
{
  "status": 206,
  "error_code": "AQS003",
  "error_msg": "Unclear Input address"
}
```

**Resolution**: Provide complete, valid Spanish address with street number and city.

---

### Use Case 5: Lyntia Web Service Failure ❌

**Scenario**: Internal quotation system error during processing.

**Response**: `206 Partial Content`
```json
{
  "status": 206,
  "error_code": "AQS005",
  "error_msg": "Autoquotation System Internal Error"
}
```

**Resolution**: Retry request or contact Lyntia support if persists.

---

### Use Case 6: Offer Not Registered in CRM ❌

**Scenario**: Quote generated but failed to save in Lyntia's CRM.

**Response**: `206 Partial Content`
```json
{
  "status": 206,
  "error_code": "AQS006",
  "error_msg": "Autoquotation System Internal Error - CRM registration failed"
}
```

**Impact**: Quote may need to be regenerated for tracking purposes.

---

### Use Case 7: Quotation Info Not Delivered ❌

**Scenario**: Quote registered but response delivery failed.

**Response**: Timeout or partial response without offer code.

**Resolution**: 
1. Check if requestID was provided
2. Contact support with requestID to retrieve quote
3. Retry with same requestID

---

## Practical Examples

### Example 1: Small Business Internet Connection

**Requirement**: 50 Mbps internet service in Barcelona for 24 months

```json
{
  "address": "Passeig de Gràcia 100, Barcelona",
  "client": "small-business-bcn",
  "service": "internet",
  "carrier": "cbb9a0a7-5d77-e911-a84f-000d3a2a78db",
  "capacityMbps": 50,
  "termMonths": 24,
  "offNetOLO": false,
  "CIDR": 29,
  "requestID": "SMB-BCN-2024-001"
}
```

### Example 2: Enterprise High-Speed Connection

**Requirement**: 10 Gbps capacity service in Madrid for 60 months

```json
{
  "address": "Castellana 200, Madrid",
  "client": "enterprise-mad",
  "service": "capacidad",
  "carrier": "ccb9a0a7-5d77-e911-a84f-000d3a2a78db",
  "capacityMbps": 10000,
  "termMonths": 60,
  "offNetOLO": true,
  "CIDR": 28,
  "requestID": "ENT-MAD-2024-10G"
}
```

### Example 3: Point-to-Point Connection

**Requirement**: 1 Gbps point-to-point link for 36 months

```json
{
  "address": "Diagonal 640, Barcelona",
  "client": "p2p-customer",
  "service": "capacidad",
  "carrier": "cbb9a0a7-5d77-e911-a84f-000d3a2a78db",
  "capacityMbps": 1000,
  "termMonths": 36,
  "offNetOLO": false,
  "CIDR": 30,  // Point-to-point subnet
  "requestID": "P2P-BCN-2024-001"
}
```

## Best Practices

### 1. Always Include RequestID
- Helps track quotes across systems
- Required for audit trails
- Format: `{CLIENT}-{LOCATION}-{YEAR}-{SEQUENCE}`

### 2. Validate Before Submitting
- Check carrier availability first
- Verify address is in Spain
- Use only valid capacity/term combinations

### 3. Error Handling Strategy
```javascript
async function createQuoteWithRetry(request, maxRetries = 3) {
  for (let i = 0; i < maxRetries; i++) {
    try {
      const response = await createQuote(request);
      if (response.status === 200) {
        return response;
      }
      if (response.status === 206) {
        // Handle partial success
        logWarning(response.error_code, response.error_msg);
      }
    } catch (error) {
      if (i === maxRetries - 1) throw error;
      await sleep(2000 * (i + 1)); // Exponential backoff
    }
  }
}
```

### 4. Address Format Guidelines
- **Good**: "Gran Vía 39, 28013 Madrid"
- **Good**: "Passeig de Gràcia 100, 08008 Barcelona"
- **Bad**: "Gran Via Madrid" (missing number)
- **Bad**: "Main Street, London" (outside Spain)

### 5. Capacity Selection
If exact capacity not available, system auto-selects nearest:
- Request 150 Mbps → System offers 200 Mbps
- Request 250 Mbps → System offers 300 Mbps
- Request 15 Mbps → System offers 20 Mbps

## Troubleshooting Guide

| Problem | Possible Cause | Solution |
|---------|---------------|----------|
| 401 Unauthorized | Invalid API key | Check Bearer token |
| 403 Forbidden | Expired token | Refresh authentication |
| Address validation fails | Non-Spanish address | Only Spanish addresses supported |
| No carriers returned | API connectivity issue | Check network/VPN connection |
| Quote takes long time | Complex routing calculation | Normal for off-net locations |
| Different capacity returned | Exact match not available | System provides nearest available |

## Support Contacts

- **Technical Issues**: Contact ConnectBase support
- **API Access**: Request through API Manager portal
- **Quote Clarifications**: Include offerCode and requestID
- **SLA Questions**: Refer to contract terms

## Appendix: Quick Reference

### Available Services
- `capacidad` - Dedicated capacity
- `internet` - Internet service

### Available Capacities (Mbps)
```
1, 2, 3, 5, 10, 20, 30, 40, 50, 60, 70, 80, 90, 100,
200, 300, 400, 500, 600, 700, 800, 900, 1000, 2000,
2500, 3000, 4000, 5000, 6000, 7000, 8000, 9000,
10000, 20000, 30000, 40000, 50000, 100000
```

### Contract Terms
- 12, 24, 36, 48, 60 months

### CIDR Options
- `/28` - 14 usable IPs
- `/29` - 6 usable IPs  
- `/30` - 2 usable IPs (P2P)