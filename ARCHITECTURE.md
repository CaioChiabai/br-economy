# Architecture Overview

## System Design

The Brazilian Economic Indicators API is designed as a high-performance caching proxy layer between clients and Brazilian government APIs. It solves three major problems:

1. **Instability**: Government APIs can be unreliable
2. **Slowness**: Direct requests to government APIs can be slow
3. **Rate Limiting**: Government APIs have strict request limits

## Architecture Diagram

```
┌─────────────┐
│   Clients   │
└──────┬──────┘
       │
       ▼
┌─────────────────────────────────────────┐
│         Rate Limiter Middleware         │
│    (100 requests/min configurable)      │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│       Express.js Application            │
│  ┌───────────────────────────────────┐  │
│  │  Controllers                      │  │
│  │  - Health Check                   │  │
│  │  - Indicator Endpoints            │  │
│  └────────────┬──────────────────────┘  │
│               │                          │
│               ▼                          │
│  ┌───────────────────────────────────┐  │
│  │  Services Layer                   │  │
│  │  - Indicator Service              │  │
│  │  - Cache Service                  │  │
│  └────────────┬──────────────────────┘  │
│               │                          │
└───────────────┼──────────────────────────┘
                │
                ▼
       ┌────────┴────────┐
       │                 │
       ▼                 ▼
┌──────────────┐  ┌──────────────┐
│  Node Cache  │  │  BCB API     │
│  (Memory)    │  │ (External)   │
└──────────────┘  └──────────────┘
```

## Key Components

### 1. Application Layer (`src/app.ts`)
- **Responsibility**: Express app configuration
- **Features**:
  - CORS enabled for cross-origin requests
  - Rate limiting middleware
  - Error handling middleware
  - Request validation

### 2. Controllers (`src/controllers/`)
- **HealthController**: System health checks
- **IndicatorController**: Economic indicator endpoints
  - List all indicators
  - Get indicator data with date range
  - Get latest indicator value

### 3. Services Layer (`src/services/`)

#### Cache Service (`cache.service.ts`)
- In-memory caching using `node-cache`
- Configurable TTL per indicator
- Cache statistics tracking
- Object cloning to prevent cache pollution

#### Indicator Service (`indicator.service.ts`)
- Integration with BCB (Banco Central do Brasil) API
- Retry logic with exponential backoff
- Configurable timeout and max retries
- Response transformation

### 4. Middleware (`src/middlewares/`)
- **Error Handler**: Centralized error handling
  - Operational errors (4xx)
  - System errors (5xx)
  - Environment-aware logging
- **Not Found Handler**: 404 responses

### 5. Configuration (`src/config/`)
- Environment-based configuration
- Sensible defaults
- Easy to override via `.env` file

## Data Flow

### Successful Cache Hit
```
Client Request
    ↓
Rate Limiter ✓
    ↓
Validation ✓
    ↓
Cache Check → [HIT]
    ↓
Return Cached Data
```

### Cache Miss
```
Client Request
    ↓
Rate Limiter ✓
    ↓
Validation ✓
    ↓
Cache Check → [MISS]
    ↓
External API Request
    ↓ (retry on failure)
External API Response
    ↓
Store in Cache
    ↓
Return Fresh Data
```

### Error Handling
```
Client Request
    ↓
Rate Limiter ✓
    ↓
Validation ✗
    ↓
400 Bad Request

OR

External API ✗
    ↓
Retry 1 (wait 1s)
    ↓
Retry 2 (wait 2s)
    ↓
Retry 3 (wait 4s)
    ↓
500 Internal Error
```

## Caching Strategy

### TTL Configuration
Different indicators have different update frequencies:

| Indicator | Update Frequency | Cache TTL |
|-----------|------------------|-----------|
| SELIC     | Monthly          | 1 hour    |
| IPCA      | Monthly          | 1 hour    |
| CDI       | Daily            | 1 hour    |
| IGP-M     | Monthly          | 1 hour    |
| Dólar     | Real-time        | 5 minutes |

### Cache Key Strategy
Cache keys are generated based on:
- Indicator type
- Start date (if provided)
- End date (if provided)

Format: `{type}_{startDate}_{endDate}`

Examples:
- `selic_all_latest` - All SELIC data
- `selic_01/01/2024_31/12/2024` - SELIC for 2024
- `dolar_latest` - Latest dollar rate

## Rate Limiting

### Default Configuration
- **Window**: 60 seconds
- **Max Requests**: 100
- **Strategy**: Fixed window

### Response Headers
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: [timestamp]
```

### Exceeded Limit Response
```json
{
  "status": "error",
  "message": "Too many requests from this IP, please try again later."
}
```

## Retry Strategy

### Configuration
- **Max Retries**: 3 (default)
- **Backoff**: Exponential
- **Formula**: `delay = 2^retryCount * 1000ms`

### Retry Schedule
1. Initial request fails
2. Wait 1 second → Retry 1
3. Wait 2 seconds → Retry 2
4. Wait 4 seconds → Retry 3
5. If still failing → Return error

## Security

### Input Validation
- Indicator type validation against enum
- Date format validation (DD/MM/YYYY)
- Query parameter sanitization

### Error Handling
- No sensitive information in production errors
- Detailed errors only in development mode
- Stack traces hidden in production

### CORS
- Enabled for all origins
- Suitable for public API

### GitHub Actions
- Minimal GITHUB_TOKEN permissions
- Secure CI/CD pipeline

## Performance Optimizations

1. **Memory Cache**: Fast in-memory caching
2. **Connection Pooling**: Axios client reuse
3. **Efficient Data Structure**: Minimal object cloning
4. **Rate Limiting**: Prevents abuse and overload
5. **Retry Logic**: Handles temporary failures without manual intervention

## Monitoring

### Health Check Endpoint
```
GET /health
```

Returns:
- Service status (healthy/degraded/unhealthy)
- Uptime in seconds
- Cache statistics (active, number of keys)
- Timestamp

### Metrics Available
- Cache hit/miss ratio (via cache stats)
- Total cache keys
- Process uptime
- Response times (via logs)

## Scalability

### Horizontal Scaling
The API is stateless (cache is instance-local), making it suitable for:
- Load balancing across multiple instances
- Container orchestration (Kubernetes)
- Serverless deployments

For shared cache across instances, consider:
- Redis as external cache
- Memcached as distributed cache

### Vertical Scaling
- Node.js single-threaded
- Consider cluster mode for multi-core servers
- Memory cache size grows with usage

## Technology Stack

- **Runtime**: Node.js 20
- **Language**: TypeScript 5.x
- **Framework**: Express 5.x
- **HTTP Client**: Axios
- **Cache**: node-cache
- **Testing**: Jest
- **Container**: Docker
- **CI/CD**: GitHub Actions

## Future Enhancements

1. **Redis Integration**: Shared cache across instances
2. **Prometheus Metrics**: Detailed monitoring
3. **GraphQL API**: Alternative query interface
4. **WebSocket**: Real-time updates
5. **More Indicators**: Expand to additional economic data
6. **Authentication**: API key management
7. **Usage Analytics**: Track API usage patterns
