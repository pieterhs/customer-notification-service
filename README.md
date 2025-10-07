# CustomerNotificationService

A backend service for creating, scheduling, and delivering customer notifications across channels (email, SMS, push). Includes templating, queueing, retries, and auditing.

## Tech Stack
- .NET 8 (ASP.NET Core)
- EF Core (Npgsql / PostgreSQL)
- Serilog
- Swagger / OpenAPI

## Project Structure
- `src/CustomerNotificationService.Api` — ASP.NET Core Web API (controllers, DI, Serilog, Swagger)
- `src/CustomerNotificationService.Application` — Application layer (services, interfaces)
- `src/CustomerNotificationService.Domain` — Domain entities and enums
- `src/CustomerNotificationService.Infrastructure` — EF Core DbContext, repositories, providers, queue (in-memory)
- `src/CustomerNotificationService.Workers` — Background workers (queue processor and scheduler)
- `tests/CustomerNotificationService.Tests` — xUnit tests (service and template basics)
- `docs/` — API contract and schema

## Prereqs
- .NET SDK 8.0+
- PostgreSQL (optional for now; default connection string points to localhost)

## Configure
Set connection string via either `appsettings.json` or environment variable `POSTGRES_CONNECTION`.

## Build
Run from repo root:

```powershell
dotnet build
```

## Run
- API:

```powershell
dotnet run --project .\src\CustomerNotificationService.Api\CustomerNotificationService.Api.csproj
```

- Workers:

```powershell
dotnet run --project .\src\CustomerNotificationService.Workers\CustomerNotificationService.Workers.csproj
```

Swagger UI will be available at http://localhost:5000/swagger in Development.

## Test

```powershell
dotnet test -v minimal
```

## Docker Quickstart

To run the entire solution with PostgreSQL using Docker:

```powershell
# Build and start all services
docker-compose up --build

# Or run in background
docker-compose up --build -d

# View logs
docker-compose logs -f api

# Stop services
docker-compose down

# Stop and remove volumes (clears database)
docker-compose down -v
```

The API will be available at:
- Main API: http://localhost:8080
- Swagger UI: http://localhost:8080/swagger  
- Health check: http://localhost:8080/health

## Features Implemented

✅ **Minimal Happy Path Complete:**
- Domain entities: Template, Notification, NotificationQueueItem, DeliveryAttempt
- EF Core with PostgreSQL and enum conversions
- EF Core migrations generated
- NotificationRepository with CreateNotificationAsync, GetCustomerHistoryAsync
- QueueRepository with EnqueueAsync, DequeueAsync (SKIP LOCKED simulation), CompleteAsync, FailAsync
- NotificationService.SendAsync: validates, persists notification, enqueues job
- API endpoint POST /api/notifications/send (returns 202 + notificationId)
- QueueWorker: polls queue, renders templates with Scriban, calls providers, records attempts
- Exponential backoff retry logic (2^attempt minutes)
- API Key middleware (X-Api-Key header)
- Template rendering with Scriban placeholders
- Comprehensive unit and integration tests

## API Endpoints

### POST /api/notifications/send
Send a notification to a recipient via the specified channel. Supports idempotency via `Idempotency-Key` header.

**Example Request:**
```bash
curl -H "X-Api-Key: dev-api-key-12345" \
     -H "Idempotency-Key: order-123" \
     -H "Content-Type: application/json" \
     -d '{
         "customerId": "11111111-2222-3333-4444-555555555555",
         "recipient": "user@example.com",
         "templateKey": "welcome",
         "channel": 0,
         "payloadJson": "{\"name\": \"Pieter\"}",
         "sendAt": "2025-10-07T18:00:00Z"
     }' \
     http://localhost:8080/api/notifications/send
```

**Example Response (202 Accepted):**
```json
{
    "notificationId": "12345678-1234-1234-1234-123456789abc",
    "status": "Scheduled",
    "scheduledAt": "2025-10-07T18:00:00Z",
    "idempotencyKey": "order-123",
    "isExisting": false
}
```

### GET /api/notifications/customer/{customerId}/history
Get paginated notification history for a specific customer with optional filtering.

**Example Request:**
```bash
curl -H "X-Api-Key: dev-api-key-12345" \
     "http://localhost:8080/api/notifications/customer/11111111-2222-3333-4444-555555555555/history?status=Sent&page=1&pageSize=20"
```

**Example Response:**
```json
{
    "items": [
        {
            "notificationId": "12345678-1234-1234-1234-123456789abc",
            "customerId": "11111111-2222-3333-4444-555555555555",
            "templateId": "welcome",
            "channel": "Email",
            "status": "Sent",
            "attemptCount": 1,
            "lastError": null,
            "createdAt": "2025-10-07T19:55:29.120748+00:00",
            "scheduledAt": null,
            "sentAt": "2025-10-07T19:55:30.866886+00:00",
            "failedAt": null,
            "renderedPreview": "Welcome Pieter!"
        }
    ],
    "page": 1,
    "pageSize": 20,
    "totalItems": 1,
    "totalPages": 1,
    "hasNext": false,
    "hasPrevious": false
}
```

**Query Parameters:**
- `status` - Filter by notification status (Pending, Sent, Failed, Scheduled)
- `from` - Start date filter (ISO 8601 format)
- `to` - End date filter (ISO 8601 format)
- `page` - Page number (default: 1, minimum: 1)
- `pageSize` - Page size (default: 20, range: 1-100)

### Health Endpoints
- `GET /health/live` - Basic liveness check (no auth required)
- `GET /health/ready` - Readiness check with database connectivity (no auth required)

### Documentation
- `GET /swagger` - Interactive Swagger UI documentation

**API Key:** Use `X-Api-Key: dev-api-key-12345` header for authenticated endpoints.

## Test Coverage

- ✅ Template rendering with placeholders
- ✅ NotificationService enqueues jobs correctly  
- ✅ End-to-end workflow (send → queue → process)
- ✅ Queue retry logic with exponential backoff
- ✅ Database transaction handling

## Run Tests

```powershell
dotnet test --verbosity minimal
```

**Current: 6/6 tests passing**

## Standalone Development

For local development without Docker, ensure you have PostgreSQL running and update the connection string in `appsettings.json`.

## Notes
EF Core migrations are applied automatically on startup (configurable via `ApplyMigrations`).
Docker setup uses PostgreSQL with credentials: user=notify, password=notify, database=notify.

## Retry Policy Configuration

The retry logic for failed notification deliveries is configurable via `RetryPolicy` section in `appsettings.json`:

```
"RetryPolicy": {
	"MaxAttempts": 5,
	"BaseBackoffSeconds": 30,
	"MaxBackoffSeconds": 3600
}
```

- **MaxAttempts**: Maximum number of delivery attempts before giving up
- **BaseBackoffSeconds**: Initial backoff delay (seconds)
- **MaxBackoffSeconds**: Maximum backoff delay (seconds)
- Backoff formula: `min(2^attempt * BaseBackoffSeconds, MaxBackoffSeconds)`

## SchedulerWorker

The SchedulerWorker runs every 30 seconds and promotes scheduled notifications:

- Finds notifications with `Status = Scheduled` and `SendAt <= now`
- Skips notifications already in the queue
- For each eligible notification:
	- Adds a `NotificationQueueItem` (`NotificationId`, `ReadyAt=now`, `AttemptCount=0`, `Status='Queued'`)
	- Updates `Notification.Status = Pending`
- All changes are saved atomically in a single transaction
- Logs which notifications were promoted

## Notification History Endpoint

`GET /api/notifications/{customerId}/history`

Returns all notifications for a customer, including delivery attempts:

**Example response:**
```json
[
	{
		"notificationId": "b1e2...",
		"templateKey": "welcome",
		"subject": "Welcome!",
		"status": "Sent",
		"sendAt": "2025-10-04T08:00:00+02:00",
		"attempts": [
			{
				"attemptedAt": "2025-10-04T08:00:01+02:00",
				"status": "Success",
				"errorMessage": null
			}
		]
	}
]
```

Returns 404 if no notifications exist for the customer.

## Swagger

Swagger UI is always available at `/swagger` in all environments (including Production/Docker).

## Quickstart

### 1. Start the complete system with Docker
```bash
docker-compose up --build
```

This will start:
- PostgreSQL database on port 5432
- API service on port 8080
- Background workers for processing notifications
- Automatic database migrations

### 2. Explore the API documentation
Open your browser to:
- **Swagger UI**: http://localhost:8080/swagger
- **Health Check**: http://localhost:8080/health/ready

### 3. Test the API with curl

**Send a notification:**
```bash
curl -H "X-Api-Key: dev-api-key-12345" \
     -H "Idempotency-Key: order-123" \
     -H "Content-Type: application/json" \
     -d '{
         "customerId": "11111111-2222-3333-4444-555555555555",
         "recipient": "user@example.com", 
         "templateKey": "welcome",
         "channel": 0,
         "payloadJson": "{\"name\": \"Pieter\"}",
         "sendAt": null
     }' \
     http://localhost:8080/api/notifications/send
```

**Get notification history:**
```bash
curl -H "X-Api-Key: dev-api-key-12345" \
     "http://localhost:8080/api/notifications/customer/11111111-2222-3333-4444-555555555555/history"
```

**Test idempotency (run the send command again with same Idempotency-Key):**
```bash
# This will return 200 OK with isExisting: true
curl -H "X-Api-Key: dev-api-key-12345" \
     -H "Idempotency-Key: order-123" \
     -H "Content-Type: application/json" \
     -d '{
         "customerId": "11111111-2222-3333-4444-555555555555",
         "recipient": "user@example.com",
         "templateKey": "welcome", 
         "channel": 0,
         "payloadJson": "{\"name\": \"Pieter\"}"
     }' \
     http://localhost:8080/api/notifications/send
```

### 4. Monitor the system
```bash
# View API logs
docker-compose logs -f api

# View worker logs
docker-compose logs -f workers

# Check container health
docker-compose ps
```

### 5. Clean up
```bash
# Stop services
docker-compose down

# Stop and remove database volumes
docker-compose down -v
```

That's it! You now have a fully functional notification service with idempotency, pagination, health checks, and comprehensive API documentation. 🚀
