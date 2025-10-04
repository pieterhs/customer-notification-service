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

- `GET /health` - Health check (no auth required)
- `POST /api/notifications/send` - Send notification (requires X-Api-Key header)
- `GET /swagger` - Swagger UI (Development only)

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
