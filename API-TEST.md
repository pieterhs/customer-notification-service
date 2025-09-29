# Customer Notification Service - API Testing

## Quick API Test (requires API running)

### 1. Start the API
```powershell
dotnet run --project .\src\CustomerNotificationService.Api\CustomerNotificationService.Api.csproj
```

### 2. Test API endpoints with curl

**Health Check (no API key required):**
```bash
curl http://localhost:5000/health
```

**Send Notification (requires API key):**
```bash
curl -X POST http://localhost:5000/api/notifications/send \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-api-key-12345" \
  -d '{
    "recipient": "john@example.com",
    "templateKey": null,
    "subject": "Test Subject",
    "body": "This is a test notification",
    "payloadJson": null,
    "channel": "Email",
    "sendAt": null,
    "customerId": "customer123"
  }'
```

**Send Notification with Template:**
```bash
curl -X POST http://localhost:5000/api/notifications/send \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: dev-api-key-12345" \
  -d '{
    "recipient": "jane@example.com",
    "templateKey": "welcome",
    "subject": null,
    "body": null,
    "payloadJson": "{\"customer_name\":\"Jane Doe\"}",
    "channel": "Email",
    "sendAt": "2025-09-30T10:00:00Z",
    "customerId": "customer456"
  }'
```

### Expected Responses

**Success (202 Accepted):**
```json
{
  "notificationId": "12345678-1234-1234-1234-123456789abc"
}
```

**Error (400 Bad Request):**
```json
{
  "error": "Recipient is required"
}
```

**Unauthorized (401):**
```
Invalid API Key
```

## Swagger UI
Visit http://localhost:5000/swagger when running in Development mode.