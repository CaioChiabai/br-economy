# API Documentation

## Base URL
```
http://localhost:3000
```

## Endpoints

### 1. Root Endpoint
Get API information and available endpoints.

**Request:**
```http
GET /
```

**Response:**
```json
{
  "name": "Brazilian Economic Indicators API",
  "version": "1.0.0",
  "description": "High-performance API Hub for Brazilian Economic Indicators",
  "endpoints": {
    "health": "/health",
    "indicators": "/api/indicators",
    "getIndicator": "/api/indicators/:type",
    "getLatest": "/api/indicators/:type/latest"
  },
  "availableIndicators": ["selic", "ipca", "cdi", "igpm", "dolar"]
}
```

---

### 2. Health Check
Check the health status of the API and cache system.

**Request:**
```http
GET /health
```

**Response:**
```json
{
  "status": "healthy",
  "timestamp": "2024-01-27T21:30:00.000Z",
  "uptime": 3600,
  "cache": {
    "status": "active",
    "keys": 15
  }
}
```

**Status Codes:**
- `200 OK` - Service is healthy
- `503 Service Unavailable` - Service is unhealthy

---

### 3. List Indicators
Get a list of all available economic indicators.

**Request:**
```http
GET /api/indicators
```

**Response:**
```json
{
  "indicators": [
    {
      "type": "selic",
      "name": "Taxa SELIC",
      "description": "Taxa básica de juros da economia brasileira"
    },
    {
      "type": "ipca",
      "name": "IPCA",
      "description": "Índice Nacional de Preços ao Consumidor Amplo"
    },
    {
      "type": "cdi",
      "name": "CDI",
      "description": "Certificado de Depósito Interbancário"
    },
    {
      "type": "igpm",
      "name": "IGP-M",
      "description": "Índice Geral de Preços do Mercado"
    },
    {
      "type": "dolar",
      "name": "Dólar",
      "description": "Taxa de câmbio Dólar Americano/Real"
    }
  ],
  "endpoints": {
    "list": "/api/indicators",
    "get": "/api/indicators/:type",
    "latest": "/api/indicators/:type/latest"
  }
}
```

---

### 4. Get Indicator Data
Retrieve historical data for a specific economic indicator.

**Request:**
```http
GET /api/indicators/:type?startDate=DD/MM/YYYY&endDate=DD/MM/YYYY
```

**Parameters:**
- `type` (path, required): Indicator type. One of: `selic`, `ipca`, `cdi`, `igpm`, `dolar`
- `startDate` (query, optional): Start date in DD/MM/YYYY format
- `endDate` (query, optional): End date in DD/MM/YYYY format

**Examples:**
```bash
# Get all SELIC data
GET /api/indicators/selic

# Get SELIC data for a specific period
GET /api/indicators/selic?startDate=01/01/2024&endDate=31/12/2024

# Get IPCA data from a start date
GET /api/indicators/ipca?startDate=01/06/2024
```

**Response:**
```json
{
  "indicator": "selic",
  "data": [
    {
      "date": "01/01/2024",
      "value": 11.75
    },
    {
      "date": "02/01/2024",
      "value": 11.75
    }
  ],
  "cached": true,
  "lastUpdate": "2024-01-27T21:30:00.000Z"
}
```

**Fields:**
- `indicator`: The requested indicator type
- `data`: Array of date-value pairs
- `cached`: Boolean indicating if data was served from cache
- `lastUpdate`: ISO timestamp of when the response was generated

**Status Codes:**
- `200 OK` - Success
- `400 Bad Request` - Invalid indicator type
- `500 Internal Server Error` - External API error

---

### 5. Get Latest Indicator Value
Retrieve the most recent value for a specific indicator.

**Request:**
```http
GET /api/indicators/:type/latest
```

**Parameters:**
- `type` (path, required): Indicator type. One of: `selic`, `ipca`, `cdi`, `igpm`, `dolar`

**Examples:**
```bash
GET /api/indicators/selic/latest
GET /api/indicators/dolar/latest
```

**Response:**
```json
{
  "indicator": "selic",
  "data": {
    "date": "27/01/2024",
    "value": 11.75
  },
  "timestamp": "2024-01-27T21:30:00.000Z"
}
```

**Status Codes:**
- `200 OK` - Success
- `400 Bad Request` - Invalid indicator type
- `404 Not Found` - No data available
- `500 Internal Server Error` - External API error

---

## Rate Limiting

The API implements rate limiting to prevent abuse:

- **Window:** 60 seconds (configurable)
- **Max Requests:** 100 requests per window (configurable)
- **Response:** HTTP 429 with retry information when limit exceeded

**Rate Limit Headers:**
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1706389800
```

---

## Caching

The API implements intelligent caching to improve performance and reduce load on external government APIs:

| Indicator | Cache TTL |
|-----------|-----------|
| SELIC     | 1 hour    |
| IPCA      | 1 hour    |
| CDI       | 1 hour    |
| IGP-M     | 1 hour    |
| DÓLAR     | 5 minutes |

Cached responses include a `cached: true` field in the response body.

---

## Error Responses

All errors follow a consistent format:

```json
{
  "status": "error",
  "message": "Error description"
}
```

**Common Error Codes:**
- `400 Bad Request` - Invalid input parameters
- `404 Not Found` - Resource not found
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server or external API error
- `503 Service Unavailable` - Service is unhealthy

---

## CORS

The API supports Cross-Origin Resource Sharing (CORS) for all origins, allowing it to be consumed by web applications from any domain.

---

## Data Sources

All economic indicators are sourced from official Brazilian government APIs:

- **Banco Central do Brasil (BCB)**: SELIC, CDI, DÓLAR
- **IBGE**: IPCA, IGP-M

The API acts as a caching layer and resilience mechanism over these sources, providing:
- Automatic retry on failures
- Exponential backoff for rate limiting
- Consistent response format
- High availability through caching

---

## Best Practices

1. **Use the latest endpoint** when you only need the most recent value
2. **Specify date ranges** to minimize data transfer
3. **Monitor the `cached` field** to understand data freshness
4. **Implement retry logic** on 5xx errors with exponential backoff
5. **Respect rate limits** by caching responses on your end when possible
6. **Check the health endpoint** before making bulk requests

---

## Support

For issues, feature requests, or questions, please open an issue on the GitHub repository.
