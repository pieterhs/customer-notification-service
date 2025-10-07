# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY src/CustomerNotificationService.Api/*.csproj ./src/CustomerNotificationService.Api/
COPY src/CustomerNotificationService.Application/*.csproj ./src/CustomerNotificationService.Application/
COPY src/CustomerNotificationService.Domain/*.csproj ./src/CustomerNotificationService.Domain/
COPY src/CustomerNotificationService.Infrastructure/*.csproj ./src/CustomerNotificationService.Infrastructure/

# Restore dependencies
RUN dotnet restore src/CustomerNotificationService.Api/CustomerNotificationService.Api.csproj

# Copy source code
COPY . .

# Publish the API project
RUN dotnet publish src/CustomerNotificationService.Api/CustomerNotificationService.Api.csproj -c Release -o /app/out

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

EXPOSE 8080

# Add health check
HEALTHCHECK --interval=30s --timeout=5s --retries=5 \
    CMD curl -f http://localhost:8080/health/ready || exit 1

ENTRYPOINT ["dotnet", "CustomerNotificationService.Api.dll"]