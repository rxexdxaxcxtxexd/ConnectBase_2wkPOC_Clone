# Colt eBonding API - Technical Specification

## API Overview

The Colt eBonding API provides a RESTful interface for integrating with Colt's telecommunications infrastructure, enabling automated service provisioning, order management, and operational support systems integration.

### Base Information

- **API Version**: 5.7
- **Protocol**: HTTPS
- **Base URL**: `https://api.colt.net/ebonding/v5`
- **Content Type**: `application/json`
- **Character Encoding**: UTF-8

## Authentication

### OAuth 2.0 Implementation

The API uses OAuth 2.0 bearer tokens for authentication.

**Token Endpoint**: `/auth/token`

**Request**:
```http
POST /auth/token HTTP/1.1
Host: api.colt.net
Content-Type: application/x-www-form-urlencoded

grant_type=client_credentials&
client_id={CLIENT_ID}&
client_secret={CLIENT_SECRET}&
scope=ebonding.read ebonding.write
```

**Response**:
```json
{
  "access_token": "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9...",
  "token_type": "Bearer",
  "expires_in": 3600,
  "scope": "ebonding.read ebonding.write"
}
```

### API Key Authentication (Legacy)

For backward compatibility, API key authentication is supported:

**Header**: `X-API-Key: {API_KEY}`

## Core API Endpoints

### 1. Service Availability

#### Check Service Availability
**Endpoint**: `POST /availability/check`

**Request**:
```json
{
  "locations": [
    {
      "type": "address",
      "address": {
        "streetAddress": "1 Canada Square",
        "city": "London",
        "postalCode": "E14 5AB",
        "country": "GB"
      }
    },
    {
      "type": "coordinates",
      "coordinates": {
        "latitude": 51.5074,
        "longitude": -0.1278
      }
    }
  ],
  "serviceType": "ethernet",
  "bandwidth": "1000",
  "bandwidthUnit": "Mbps"
}
```

**Response**:
```json
{
  "requestId": "req-12345-67890",
  "availability": [
    {
      "locationId": "loc-001",
      "serviceable": true,
      "onNet": true,
      "services": [
        {
          "serviceType": "ethernet",
          "bandwidthOptions": ["100", "500", "1000", "10000"],
          "bandwidthUnit": "Mbps",
          "leadTime": {
            "standard": 30,
            "expedited": 15,
            "unit": "days"
          }
        }
      ],
      "buildCost": null
    }
  ],
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### 2. Quotation Management

#### Create Quote
**Endpoint**: `POST /quotes/create`

**Request**:
```json
{
  "customer": {
    "accountNumber": "CUST-12345",
    "companyName": "Example Corp",
    "contactEmail": "procurement@example.com"
  },
  "services": [
    {
      "serviceType": "ethernet",
      "bandwidth": "1000",
      "bandwidthUnit": "Mbps",
      "termMonths": 36,
      "locations": {
        "endpointA": {
          "referenceId": "loc-001",
          "siteName": "London HQ"
        },
        "endpointB": {
          "referenceId": "loc-002",
          "siteName": "Manchester DC"
        }
      },
      "protection": "protected",
      "sla": "premium"
    }
  ],
  "currency": "GBP"
}
```

**Response**:
```json
{
  "quoteId": "QT-2024-001234",
  "status": "active",
  "validUntil": "2024-02-14T23:59:59Z",
  "pricing": {
    "currency": "GBP",
    "services": [
      {
        "serviceId": "svc-001",
        "monthlyRecurringCharge": 2500.00,
        "oneTimeCharge": 1500.00,
        "installationCharge": 500.00,
        "termMonths": 36,
        "totalContractValue": 92000.00
      }
    ],
    "totals": {
      "monthlyRecurring": 2500.00,
      "oneTime": 2000.00,
      "totalContractValue": 92000.00
    }
  },
  "expiryDate": "2024-02-14T23:59:59Z"
}
```

### 3. Order Management

#### Submit Order
**Endpoint**: `POST /orders/submit`

**Request**:
```json
{
  "quoteId": "QT-2024-001234",
  "purchaseOrderNumber": "PO-98765",
  "requestedDeliveryDate": "2024-03-01",
  "technicalContacts": [
    {
      "name": "John Smith",
      "email": "john.smith@example.com",
      "phone": "+44 20 7946 0958",
      "role": "primary"
    }
  ],
  "siteAccess": {
    "endpointA": {
      "accessHours": "24x7",
      "restrictions": "None",
      "contactName": "Site Manager A",
      "contactPhone": "+44 20 7946 0959"
    },
    "endpointB": {
      "accessHours": "Mon-Fri 8am-6pm",
      "restrictions": "48hr notice required",
      "contactName": "Site Manager B",
      "contactPhone": "+44 161 123 4567"
    }
  }
}
```

**Response**:
```json
{
  "orderId": "ORD-2024-005678",
  "status": "submitted",
  "expectedCompletionDate": "2024-03-01",
  "orderItems": [
    {
      "itemId": "item-001",
      "serviceId": "svc-001",
      "status": "pending_validation"
    }
  ],
  "nextMilestone": {
    "name": "Technical Survey",
    "expectedDate": "2024-01-20"
  }
}
```

#### Get Order Status
**Endpoint**: `GET /orders/{orderId}/status`

**Response**:
```json
{
  "orderId": "ORD-2024-005678",
  "overallStatus": "in_progress",
  "percentComplete": 45,
  "milestones": [
    {
      "name": "Order Validation",
      "status": "completed",
      "completedDate": "2024-01-16T09:00:00Z"
    },
    {
      "name": "Technical Survey",
      "status": "completed",
      "completedDate": "2024-01-20T14:30:00Z"
    },
    {
      "name": "Network Design",
      "status": "in_progress",
      "expectedDate": "2024-01-25T17:00:00Z"
    },
    {
      "name": "Installation",
      "status": "pending",
      "expectedDate": "2024-02-15T10:00:00Z"
    },
    {
      "name": "Testing & Commissioning",
      "status": "pending",
      "expectedDate": "2024-02-20T12:00:00Z"
    },
    {
      "name": "Service Activation",
      "status": "pending",
      "expectedDate": "2024-03-01T09:00:00Z"
    }
  ],
  "currentPhase": "network_design",
  "estimatedCompletionDate": "2024-03-01"
}
```

### 4. Service Management

#### Get Service Details
**Endpoint**: `GET /services/{serviceId}`

**Response**:
```json
{
  "serviceId": "SVC-ETHERNET-123456",
  "status": "active",
  "serviceType": "ethernet",
  "configuration": {
    "bandwidth": "1000",
    "bandwidthUnit": "Mbps",
    "interface": "1000Base-LX",
    "vlan": {
      "type": "tagged",
      "id": 100
    },
    "endpoints": [
      {
        "locationId": "loc-001",
        "deviceName": "CE-LON-001",
        "ipAddress": "10.0.1.1/30",
        "macAddress": "00:1B:44:11:3A:B7"
      },
      {
        "locationId": "loc-002",
        "deviceName": "CE-MAN-001",
        "ipAddress": "10.0.1.2/30",
        "macAddress": "00:1B:44:11:3A:B8"
      }
    ]
  },
  "activationDate": "2024-03-01T09:15:00Z",
  "contractEndDate": "2027-03-01T00:00:00Z"
}
```

#### Modify Service
**Endpoint**: `PUT /services/{serviceId}/modify`

**Request**:
```json
{
  "modificationType": "bandwidth_upgrade",
  "newConfiguration": {
    "bandwidth": "10000",
    "bandwidthUnit": "Mbps"
  },
  "requestedDate": "2024-04-01",
  "changeReason": "Increased capacity requirements"
}
```

### 5. Fault Management

#### Create Ticket
**Endpoint**: `POST /tickets/create`

**Request**:
```json
{
  "serviceId": "SVC-ETHERNET-123456",
  "issueType": "service_degradation",
  "severity": "P2",
  "description": "Intermittent packet loss observed",
  "impactDescription": "Affecting production traffic",
  "symptoms": [
    "Packet loss averaging 2%",
    "Latency spikes up to 150ms",
    "Started at 2024-01-15T14:30:00Z"
  ],
  "contactDetails": {
    "name": "NOC Team",
    "email": "noc@example.com",
    "phone": "+44 20 7946 0960"
  }
}
```

**Response**:
```json
{
  "ticketId": "INC-2024-789012",
  "status": "open",
  "priority": "P2",
  "assignedTeam": "Network Operations",
  "estimatedResolutionTime": "2024-01-15T18:30:00Z",
  "slaTarget": {
    "responseTime": "1 hour",
    "resolutionTime": "4 hours"
  }
}
```

### 6. Billing and Usage

#### Get Usage Data
**Endpoint**: `GET /usage/{serviceId}`

**Query Parameters**:
- `startDate`: ISO 8601 date
- `endDate`: ISO 8601 date
- `granularity`: `hourly`, `daily`, `monthly`

**Response**:
```json
{
  "serviceId": "SVC-ETHERNET-123456",
  "period": {
    "start": "2024-01-01T00:00:00Z",
    "end": "2024-01-31T23:59:59Z"
  },
  "usage": [
    {
      "timestamp": "2024-01-01T00:00:00Z",
      "metrics": {
        "inboundTrafficGb": 1250.5,
        "outboundTrafficGb": 980.3,
        "peakUtilizationPercent": 78.5,
        "availabilityPercent": 100.0
      }
    }
  ],
  "summary": {
    "totalInboundGb": 38765.2,
    "totalOutboundGb": 30412.8,
    "averageUtilizationPercent": 65.3,
    "availabilityPercent": 99.98
  }
}
```

## Data Models

### Address Object
```json
{
  "streetAddress": "string",
  "streetAddress2": "string (optional)",
  "city": "string",
  "state": "string (optional)",
  "postalCode": "string",
  "country": "ISO 3166-1 alpha-2 code"
}
```

### Coordinates Object
```json
{
  "latitude": "number (-90 to 90)",
  "longitude": "number (-180 to 180)",
  "accuracy": "number (meters, optional)"
}
```

### Service Configuration
```json
{
  "serviceType": "enum [ethernet, internet, vpn, wavelength]",
  "bandwidth": "number",
  "bandwidthUnit": "enum [Mbps, Gbps]",
  "redundancy": "enum [none, protected, diverse]",
  "sla": "enum [standard, premium, platinum]",
  "cos": "enum [bronze, silver, gold, platinum]"
}
```

## Error Handling

### Error Response Format
```json
{
  "error": {
    "code": "ERR_INVALID_REQUEST",
    "message": "The request contains invalid parameters",
    "details": [
      {
        "field": "bandwidth",
        "issue": "Value must be a positive integer",
        "value": "-100"
      }
    ],
    "timestamp": "2024-01-15T10:30:00Z",
    "traceId": "trace-123456789"
  }
}
```

### Error Codes

| Code | HTTP Status | Description |
|------|-------------|-------------|
| `ERR_AUTHENTICATION_FAILED` | 401 | Invalid or expired credentials |
| `ERR_INSUFFICIENT_PERMISSIONS` | 403 | User lacks required permissions |
| `ERR_RESOURCE_NOT_FOUND` | 404 | Requested resource does not exist |
| `ERR_INVALID_REQUEST` | 400 | Request validation failed |
| `ERR_DUPLICATE_REQUEST` | 409 | Duplicate request detected |
| `ERR_RATE_LIMIT_EXCEEDED` | 429 | Too many requests |
| `ERR_SERVICE_UNAVAILABLE` | 503 | Service temporarily unavailable |
| `ERR_INTERNAL_ERROR` | 500 | Internal server error |

## Rate Limiting

### Limits
- **Standard Tier**: 100 requests per minute
- **Premium Tier**: 500 requests per minute
- **Enterprise Tier**: 2000 requests per minute

### Rate Limit Headers
```http
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1642248000
```

## Webhooks

### Webhook Configuration
**Endpoint**: `POST /webhooks/register`

**Request**:
```json
{
  "url": "https://example.com/webhooks/colt",
  "events": ["order.status_change", "ticket.created", "service.activated"],
  "secret": "webhook-secret-key",
  "active": true
}
```

### Webhook Payload
```json
{
  "eventId": "evt-123456",
  "eventType": "order.status_change",
  "timestamp": "2024-01-15T10:30:00Z",
  "data": {
    "orderId": "ORD-2024-005678",
    "previousStatus": "in_progress",
    "newStatus": "completed",
    "changedBy": "system"
  },
  "signature": "sha256=..."
}
```

### Webhook Security
Verify webhook authenticity using HMAC-SHA256:
```
signature = HMAC-SHA256(secret, requestBody)
```

## Testing Environment

### Sandbox Environment
- **Base URL**: `https://sandbox.api.colt.net/ebonding/v5`
- **Test Credentials**: Provided upon request
- **Data Refresh**: Daily at 00:00 UTC
- **Limitations**: 
  - Orders auto-cancel after 24 hours
  - Maximum 10 active services
  - Simulated latency: 100-500ms

### Test Data

#### Test Locations (Always Available)
```json
[
  {
    "name": "London Test Site",
    "address": "1 Test Street, London, EC1A 1BB, GB",
    "coordinates": {"latitude": 51.5074, "longitude": -0.1278}
  },
  {
    "name": "Paris Test Site", 
    "address": "1 Rue de Test, 75001 Paris, FR",
    "coordinates": {"latitude": 48.8566, "longitude": 2.3522}
  }
]
```

## API Versioning

### Version Strategy
- Semantic versioning (MAJOR.MINOR.PATCH)
- Backward compatibility maintained within major versions
- Deprecation notice: 6 months minimum
- Version specified in URL path

### Version Migration
```http
# Current version
GET /ebonding/v5/services

# Previous version (deprecated)
GET /ebonding/v4/services

# Version header (optional)
X-API-Version: 5.7
```

## Security Considerations

### Data Encryption
- All API communications use TLS 1.2 or higher
- Sensitive data fields encrypted at rest using AES-256
- PCI DSS compliance for payment data
- GDPR compliance for personal data

### IP Whitelisting
Configure allowed IP addresses:
```json
{
  "allowedIPs": [
    "203.0.113.0/24",
    "198.51.100.0/24"
  ]
}
```

### Audit Logging
All API requests are logged with:
- Timestamp
- User/Application ID
- IP Address
- Request/Response summary
- Response time

## Performance Guidelines

### Optimization Tips
1. Use batch operations where available
2. Implement caching for reference data
3. Utilize webhook notifications instead of polling
4. Compress large payloads using gzip
5. Use field filtering to reduce response size

### SLA Commitments
- **API Availability**: 99.9% uptime
- **Response Time**: 
  - P95 < 500ms for GET requests
  - P95 < 1000ms for POST/PUT requests
- **Throughput**: Minimum 100 TPS per customer

## Compliance and Certifications

- ISO 27001:2013 certified
- SOC 2 Type II compliant
- GDPR compliant
- PCI DSS Level 1 certified

## Support Information

### Technical Support
- **Email**: api-support@colt.net
- **Phone**: +44 20 7390 3900
- **Hours**: 24x7 for P1/P2, Business hours for P3/P4

### Documentation Resources
- API Reference: https://developer.colt.net/ebonding
- Status Page: https://status.colt.net
- Change Log: https://developer.colt.net/changelog

## Appendix

### Country Codes
Use ISO 3166-1 alpha-2 codes:
- GB - United Kingdom
- FR - France
- DE - Germany
- ES - Spain
- IT - Italy
- NL - Netherlands

### Currency Codes
Use ISO 4217 codes:
- GBP - British Pound
- EUR - Euro
- USD - US Dollar

### Time Zones
All timestamps in UTC (ISO 8601 format)