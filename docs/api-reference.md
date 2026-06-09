# Coldrun API Reference

Last updated: 2026-05-27

## Documentation Context

- Documentation type: API reference
- Target audience: API consumers and backend developers
- Scope: currently exposed Coldrun HTTP endpoints
- Output location: docs/
- Format: Markdown
- Depth: reference-focused
- Tone: formal, concise

## Base URL

Local default:

```text
http://localhost:5000
```

## Common Response Behavior

### Correlation ID

- Request header accepted: `X-Correlation-ID`
- Response header emitted: `X-Correlation-ID`

If request header is missing, server generates one.

### Error payload shape

Errors from global exception handling use:

```json
{ "error": "message" }
```

## System Endpoints

### GET /health/live

Returns process liveness and check details.

Example response:

```json
{
  "status": "Healthy",
  "correlationId": "f2b10283a73943c7b26f9e8b087244c1",
  "checks": [
    {
      "name": "inmemorystore",
      "status": "Healthy"
    }
  ]
}
```

### GET /health/ready

Returns readiness state using current health checks (in-memory store accessibility in current implementation).

Response shape is identical to `/health/live`.

### GET /metrics

Returns in-process request counters.

Example response:

```json
{
  "totalRequests": 42,
  "requestsByEndpoint": {
    "HTTP: GET /api/trucks": 10,
    "HTTP: POST /api/trucks": 3
  },
  "requestsByStatusCategory": {
    "Success": 37,
    "ClientError": 5
  }
}
```

### GET /openapi/v1.json

Returns OpenAPI JSON for the currently mapped API.

### GET /scalar/v1

Returns Scalar interactive API documentation UI.

## Trucks Endpoints

Endpoint group base path:

```text
/api/trucks
```

All trucks endpoints are under rate limiting policy `truck-api`.

## Data Contract

Truck object shape:

```json
{
  "id": "3d2358a5-cb02-4fd6-b85f-d5f6db6c5dc0",
  "code": "TRK001",
  "name": "Alpha Truck",
  "status": "Out Of Service",
  "description": "Optional text"
}
```

Valid status values:

- Out Of Service
- Loading
- To Job
- At Job
- Returning

Code rules:

- required
- unique (case-insensitive)
- alphanumeric only

### POST /api/trucks

Creates a truck.

Request body:

```json
{
  "code": "TRK001",
  "name": "Alpha Truck",
  "status": "Out Of Service",
  "description": "Primary truck"
}
```

Successful response:

- Status: `201 Created`
- Location: `/api/trucks/{id}`
- Body: truck object

Possible errors:

- `400 Bad Request` (invalid code/name/status)
- `409 Conflict` (duplicate code)

### GET /api/trucks/{id}

Gets a truck by ID.

Successful response:

- Status: `200 OK`
- Body: truck object

Not found response:

- Status: `404 Not Found`
- Body:

```json
{ "error": "Truck with id '...' not found" }
```

### GET /api/trucks

Lists trucks with optional filtering and sorting.

Query parameters:

| Name | Type | Default | Behavior |
|---|---|---|---|
| `code` | string | none | contains filter, case-insensitive |
| `name` | string | none | contains filter, case-insensitive |
| `status` | string | none | exact status filter, case-insensitive |
| `sortBy` | string | `code` | `code`, `name`, `status` |
| `sortDir` | string | `asc` | `asc` or `desc` |

Example:

```http
GET /api/trucks?status=Loading&sortBy=name&sortDir=desc
```

Successful response:

- Status: `200 OK`
- Body: array of truck objects

### PUT /api/trucks/{id}

Updates truck data.

Request body:

```json
{
  "code": "TRK001",
  "name": "Alpha Truck Updated",
  "status": "Loading",
  "description": "Updated description"
}
```

Transition behavior:

- Status changes must obey transition policy.
- Setting the same status value is allowed.

Successful response:

- Status: `200 OK`
- Body: updated truck object

Possible errors:

- `400 Bad Request` (invalid payload or disallowed status transition)
- `404 Not Found` (truck ID not found)
- `409 Conflict` (updated code conflicts with another truck)

### DELETE /api/trucks/{id}

Deletes a truck by ID.

Successful response:

- Status: `204 No Content`

Not found response:

- Status: `404 Not Found`
- Body:

```json
{ "error": "Truck with id '...' not found" }
```

## Status Transition Rules

Rules are enforced on update operations:

| Current | Allowed Next |
|---|---|
| Out Of Service | Out Of Service, Loading, To Job, At Job, Returning |
| Loading | Loading, Out Of Service, To Job |
| To Job | To Job, Out Of Service, At Job |
| At Job | At Job, Out Of Service, Returning |
| Returning | Returning, Out Of Service, Loading |

## Quick Curl Examples

Create:

```bash
curl -i -X POST http://localhost:5000/api/trucks \
  -H "Content-Type: application/json" \
  -d '{"code":"TRK777","name":"Runner","status":"Out Of Service","description":"Seeded by curl"}'
```

List:

```bash
curl -s "http://localhost:5000/api/trucks?sortBy=code&sortDir=asc"
```

Update status:

```bash
curl -i -X PUT http://localhost:5000/api/trucks/{id} \
  -H "Content-Type: application/json" \
  -d '{"code":"TRK777","name":"Runner","status":"Loading","description":"Now loading"}'
```

Delete:

```bash
curl -i -X DELETE http://localhost:5000/api/trucks/{id}
```
