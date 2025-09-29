# CustomerNotificationService API Contract

Version: 0.1 (Draft)
Base URL: https://{host}/api/v1
Auth: Bearer token via `Authorization: Bearer <token>`
Content-Type: application/json unless specified.

## Conventions
- All timestamps are ISO 8601 in UTC.
- Idempotency supported on create endpoints via `Idempotency-Key` header.
- Pagination: `?page=1&pageSize=50` with `Link` headers and `X-Total-Count`.
- Errors follow RFC 7807 problem+json.

## Security
- Authorization: `Authorization: Bearer <JWT or opaque token>`
- Optional HMAC verification for inbound webhooks using `X-Signature`.

---

## Health
GET /health
- 200 OK
{
  "status": "ok",
  "uptimeSec": 1234
}

---

## Templates
### POST /templates
Create a template.
Headers: Authorization, Idempotency-Key (optional)
Request:
{
  "name": "welcome-email",
  "channel": "email", // email | sms | push | webhook
  "locale": "en-US",
  "subject": "Welcome, {{firstName}}!",
  "body": "Hello {{firstName}}, thanks for joining.",
  "isActive": true
}
Responses:
- 201 Created
  Location: /templates/{id}
  Body:
  {
    "id": "uuid",
    "name": "welcome-email",
    "channel": "email",
    "locale": "en-US",
    "subject": "Welcome, {{firstName}}!",
    "body": "Hello {{firstName}}, thanks for joining.",
    "version": 1,
    "isActive": true,
    "createdAt": "2025-09-29T12:00:00Z",
    "updatedAt": "2025-09-29T12:00:00Z"
  }
- 409 Conflict (duplicate name+channel+locale)
- 400 Bad Request

### GET /templates
Query templates.
Query: name, channel, locale, isActive, page, pageSize
- 200 OK [
  { "id": "uuid", "name": "...", "channel": "email", "locale": "en-US", "version": 3, "isActive": true }
]
Headers: Link, X-Total-Count

### GET /templates/{id}
- 200 OK { template }
- 404 Not Found

### PUT /templates/{id}
Full replace (bump version).
Request: same shape as POST (subject optional for non-email)
- 200 OK { template }
- 409 Conflict (concurrency/version)
- 404 Not Found

### PATCH /templates/{id}
Partial update.
Request: JSON Patch or merge-patch
- 200 OK { template }
- 409 Conflict
- 404 Not Found

### DELETE /templates/{id}
Soft delete.
- 204 No Content

---

## Notifications
### POST /notifications
Create and optionally schedule a notification.
Headers: Authorization, Idempotency-Key (optional)
Request:
{
  "templateId": "uuid",       // optional if using inline template
  "channel": "email",
  "recipient": "user@example.com",
  "payload": { "firstName": "Lee" },
  "priority": "normal",       // low | normal | high | urgent
  "scheduleAt": "2025-09-30T10:00:00Z" // optional
}
Responses:
- 202 Accepted
  {
    "id": "uuid",
    "status": "pending",
    "scheduleAt": "2025-09-30T10:00:00Z",
    "createdAt": "2025-09-29T12:00:00Z"
  }
- 400 Bad Request
- 404 Not Found (template)

### GET /notifications
Query notifications.
Query: status, channel, recipient, createdFrom, createdTo, page, pageSize
- 200 OK {
  "items": [ { "id": "uuid", "status": "pending", "channel": "email", "recipient": "user@example.com", "createdAt": "..." } ],
  "page": 1,
  "pageSize": 50,
  "total": 123
}

### GET /notifications/{id}
- 200 OK {
  "id": "uuid",
  "templateId": "uuid",
  "channel": "email",
  "recipient": "user@example.com",
  "payload": { ... },
  "priority": "normal",
  "scheduleAt": null,
  "status": "processing",
  "error": { "code": null, "message": null },
  "createdAt": "...",
  "updatedAt": "..."
}
- 404 Not Found

### POST /notifications/{id}/cancel
Cancel a pending/processing notification.
- 200 OK { "id": "uuid", "status": "canceled" }
- 409 Conflict (already terminal state)
- 404 Not Found

---

## Queue (internal/admin)
### GET /queue/ready
Fetch ready queue items for workers.
Query: limit (default 50), workerId
- 200 OK [
  {
    "queueId": 123,
    "notificationId": "uuid",
    "availableAt": "2025-09-29T12:00:00Z",
    "attempt": 0
  }
]

### POST /queue/{queueId}/lock
Body: { "workerId": "dispatcher-1", "lockTtlSec": 60 }
- 200 OK { "lockedAt": "...", "lockedBy": "dispatcher-1" }
- 409 Conflict (already locked)
- 404 Not Found

### POST /queue/{queueId}/ack
Mark as processed and remove from queue.
Body: { "status": "delivered" | "failed", "nextAttemptAt": "2025-09-29T12:05:00Z" (optional for retries) }
- 200 OK { "status": "sent" }
- 404 Not Found

---

## Delivery Attempts
### GET /notifications/{id}/attempts
- 200 OK [
  {
    "id": 456,
    "channel": "email",
    "provider": "ses",
    "status": "success",
    "error": null,
    "durationMs": 120,
    "createdAt": "..."
  }
]
- 404 Not Found

---

## Audit Logs
### GET /audit
Query: entityType, entityId, from, to, page, pageSize
- 200 OK [ { "entityType": "notification", "entityId": "uuid", "action": "status_change", "createdAt": "...", "changes": { ... } } ]

---

## Error format (RFC 7807)
Content-Type: application/problem+json
{
  "type": "https://docs.example.com/errors/validation",
  "title": "Your request parameters didn't validate.",
  "status": 400,
  "detail": "'recipient' must be a valid email.",
  "instance": "/api/v1/notifications/123",
  "errors": {
    "recipient": ["Invalid email"]
  }
}
