// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# API Reference

Complete reference for the Outbox Pattern REST API.

## Base URL

```
https://localhost:5001/api
```

## Authentication

The current implementation includes no authentication. In production, add:
- API Key validation
- JWT bearer tokens
- OAuth2/OIDC
- Mutual TLS

## Response Format

All responses are JSON. Errors follow this format:

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The request body is invalid.",
  "traceId": "0HN4GFGUAA3KM:00000001"
}
```

## Outbox Messages

### Publish Event

**POST** `/outbox/events`

Publish a domain event to the outbox. The event is stored and queued for publication.

**Request Body:**
```json
{
  "event": {
    "eventType": "order.created",
    "version": 1,
    "orderId": "ORD-123",
    "customerId": "CUST-456",
    "amount": 99.99,
    "createdAt": "2024-01-15T10:00:00Z"
  },
  "topic": "orders.created",
  "partitionKey": "CUST-456",
  "idempotencyKey": "order-ORD-123-create"
}
```

**Query Parameters:**
- None

**Headers:**
- `Content-Type: application/json`

**Response:** `201 Created`
```json
{
  "id": "7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p",
  "aggregateId": "ORD-123",
  "topic": "orders.created",
  "state": "Pending",
  "createdAt": "2024-01-15T10:00:00Z",
  "retryCount": 0
}
```

**Status Codes:**
- `201 Created` - Event published successfully
- `400 Bad Request` - Invalid request body
- `409 Conflict` - Duplicate idempotency key with different data
- `500 Internal Server Error` - Database or system error

**Example (cURL):**
```bash
curl -X POST https://localhost:5001/api/outbox/events \
  -H "Content-Type: application/json" \
  -d '{
    "event": {
      "eventType": "order.created",
      "orderId": "ORD-123",
      "customerId": "CUST-456",
      "amount": 99.99
    },
    "topic": "orders.created",
    "partitionKey": "CUST-456",
    "idempotencyKey": "order-ORD-123-create"
  }' \
  -k  # Skip SSL verification for localhost
```

---

### Get Message

**GET** `/outbox/messages/{messageId}`

Retrieve details of a specific outbox message.

**Path Parameters:**
- `messageId` (required, UUID) - The message ID

**Response:** `200 OK`
```json
{
  "id": "7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p",
  "aggregateId": "ORD-123",
  "topic": "orders.created",
  "eventData": "{\"orderId\":\"ORD-123\",\"amount\":99.99}",
  "state": "Published",
  "createdAt": "2024-01-15T10:00:00Z",
  "publishedAt": "2024-01-15T10:00:05Z",
  "retryCount": 0,
  "idempotencyKey": "order-ORD-123-create",
  "partitionKey": "CUST-456"
}
```

**Status Codes:**
- `200 OK` - Message found
- `404 Not Found` - Message not found
- `500 Internal Server Error` - Database error

**Example:**
```bash
curl -X GET https://localhost:5001/api/outbox/messages/7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p -k
```

---

### Get Statistics

**GET** `/outbox/statistics`

Get current outbox statistics and health metrics.

**Response:** `200 OK`
```json
{
  "totalCount": 1500,
  "pendingCount": 23,
  "publishedCount": 1450,
  "deadLetterCount": 27,
  "successRate": 0.9733,
  "averageRetries": 1.2,
  "lastProcessedTime": "2024-01-15T14:32:45.000Z"
}
```

**Status Codes:**
- `200 OK` - Always succeeds
- `500 Internal Server Error` - Database error (rare)

**Example:**
```bash
curl -X GET https://localhost:5001/api/outbox/statistics -k | jq .
```

---

## Dead Letter Queue

### Get Unreviewed Messages

**GET** `/deadletters/unreviewed`

Retrieve all unreviewed dead letters awaiting operator intervention.

**Query Parameters:**
- `skip` (optional, int, default 0) - Number of records to skip
- `take` (optional, int, default 100) - Number of records to return

**Response:** `200 OK`
```json
{
  "items": [
    {
      "id": "9d8c7b6a-5f4e-3d2c-1b0a-f9e8d7c6b5a4",
      "originalMessageId": "7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p",
      "originalMessage": "{\"orderId\":\"ORD-123\"}",
      "lastError": "Failed to publish to RabbitMQ: Connection timeout",
      "failureCount": 5,
      "createdAt": "2024-01-15T10:00:00Z",
      "reviewedAt": null,
      "reviewedBy": null,
      "reviewNotes": null
    }
  ],
  "totalCount": 42,
  "skip": 0,
  "take": 100
}
```

**Status Codes:**
- `200 OK` - Always succeeds
- `500 Internal Server Error` - Database error

**Example:**
```bash
curl -X GET 'https://localhost:5001/api/deadletters/unreviewed?take=10' -k | jq .
```

---

### Review Dead Letter

**PUT** `/deadletters/{deadLetterId}/review`

Mark a dead letter as reviewed and record the review notes.

**Path Parameters:**
- `deadLetterId` (required, UUID) - The dead letter ID

**Request Body:**
```json
{
  "reviewNotes": "Reviewed with team - database connection issue resolved"
}
```

**Response:** `204 No Content`

**Status Codes:**
- `204 No Content` - Review recorded successfully
- `404 Not Found` - Dead letter not found
- `409 Conflict` - Dead letter already reviewed
- `500 Internal Server Error` - Database error

**Example:**
```bash
curl -X PUT https://localhost:5001/api/deadletters/9d8c7b6a-5f4e-3d2c-1b0a-f9e8d7c6b5a4/review \
  -H "Content-Type: application/json" \
  -d '{"reviewNotes":"Team approved - infrastructure restored"}' \
  -k
```

---

### Requeue Dead Letter

**PUT** `/deadletters/{deadLetterId}/requeue`

Requeue a dead letter for another attempt after the underlying issue is fixed.

**Path Parameters:**
- `deadLetterId` (required, UUID) - The dead letter ID

**Request Body:**
```json
{
  "reason": "Upstream service is now healthy, retrying message"
}
```

**Response:** `200 OK`
```json
{
  "newMessageId": "a1b2c3d4-e5f6-7g8h-9i0j-k1l2m3n4o5p6",
  "status": "Requeued",
  "message": "Dead letter requeued successfully"
}
```

**Status Codes:**
- `200 OK` - Requeue successful
- `404 Not Found` - Dead letter not found
- `409 Conflict` - Dead letter already requeued
- `500 Internal Server Error` - Database error

**Example:**
```bash
curl -X PUT https://localhost:5001/api/deadletters/9d8c7b6a-5f4e-3d2c-1b0a-f9e8d7c6b5a4/requeue \
  -H "Content-Type: application/json" \
  -d '{"reason":"Infrastructure maintenance complete"}' \
  -k
```

---

## Health & Monitoring

### Health Check

**GET** `/health`

Basic health check endpoint for orchestrators and load balancers.

**Response:** `200 OK`
```json
{
  "status": "healthy",
  "timestamp": "2024-01-15T14:32:45.000Z"
}
```

Or with statistics:
```json
{
  "status": "healthy",
  "totalCount": 1500,
  "pendingCount": 23,
  "publishedCount": 1450,
  "deadLetterCount": 27,
  "successRate": 0.9733
}
```

**Status Codes:**
- `200 OK` - Service is healthy
- `503 Service Unavailable` - Service is unhealthy
  - Too many unreviewed dead letters
  - Processing is stalled
  - Database unavailable

**Example:**
```bash
curl -X GET https://localhost:5001/health -k
```

---

## Export

### Export Messages

**GET** `/export/messages`

Export outbox messages in various formats.

**Query Parameters:**
- `format` (optional, string, default json) - csv, json, or xml
- `state` (optional, string) - Filter by state: Pending, Published, Failed
- `topic` (optional, string) - Filter by topic
- `dateFrom` (optional, ISO8601 datetime) - Filter from date
- `dateTo` (optional, ISO8601 datetime) - Filter to date

**Response:** Based on format parameter

**JSON Format:**
```json
{
  "messages": [
    {
      "id": "7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p",
      "topic": "orders.created",
      "state": "Published",
      "createdAt": "2024-01-15T10:00:00Z"
    }
  ]
}
```

**CSV Format:**
```
Id,Topic,State,CreatedAt,PublishedAt
7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p,orders.created,Published,2024-01-15T10:00:00Z,2024-01-15T10:00:05Z
```

**Status Codes:**
- `200 OK` - Export successful
- `400 Bad Request` - Invalid format or filter
- `500 Internal Server Error` - Database error

**Examples:**
```bash
# Export as JSON
curl -X GET 'https://localhost:5001/api/export/messages?format=json' -k > export.json

# Export as CSV
curl -X GET 'https://localhost:5001/api/export/messages?format=csv&state=Published' -k > export.csv

# Export as XML
curl -X GET 'https://localhost:5001/api/export/messages?format=xml&topic=orders.created' -k > export.xml
```

---

## Error Handling

### Common Error Responses

#### 400 Bad Request
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "Event data must not be null or empty"
}
```

#### 404 Not Found
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "Not Found",
  "status": 404,
  "detail": "Message with ID 7b2a1c3d-4e5f-6g7h-8i9j-0k1l2m3n4o5p not found"
}
```

#### 500 Internal Server Error
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "Database connection failed",
  "traceId": "0HN4GFGUAA3KM:00000001"
}
```

---

## Rate Limiting

Not currently implemented. Add in production:

```
X-RateLimit-Limit: 1000
X-RateLimit-Remaining: 999
X-RateLimit-Reset: 1705339200
```

---

## Pagination

Supported on list endpoints:
- `skip` - Number of records to skip (default: 0)
- `take` - Number of records to return (default: 100, max: 1000)

---

## Filtering

Available on list endpoints:
- `state` - Filter by message state
- `topic` - Filter by topic
- `createdFrom` - Filter from date (ISO8601)
- `createdTo` - Filter to date (ISO8601)

---

## Versioning

The API uses URL-based versioning. Currently v1.

Future versions:
- `/api/v2/outbox/events`
- `/api/v2/deadletters/unreviewed`

---

## Changelog

This API reference corresponds to v1.2.0. See [CHANGELOG.md](../CHANGELOG.md) for changes.

