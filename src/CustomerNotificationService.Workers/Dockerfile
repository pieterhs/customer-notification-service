# syntax=docker/dockerfile:1

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files for restore
COPY CustomerNotificationService.sln ./
COPY src/CustomerNotificationService.Domain/CustomerNotificationService.Domain.csproj src/CustomerNotificationService.Domain/
COPY src/CustomerNotificationService.Application/CustomerNotificationService.Application.csproj src/CustomerNotificationService.Application/
COPY src/CustomerNotificationService.Infrastructure/CustomerNotificationService.Infrastructure.csproj src/CustomerNotificationService.Infrastructure/
COPY src/CustomerNotificationService.Workers/CustomerNotificationService.Workers.csproj src/CustomerNotificationService.Workers/

# Restore only Workers (will restore referenced projects as well)
RUN dotnet restore src/CustomerNotificationService.Workers/CustomerNotificationService.Workers.csproj

# Copy entire source
COPY . .

# Publish
RUN dotnet publish src/CustomerNotificationService.Workers/CustomerNotificationService.Workers.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Reduce diagnostics overhead in containers
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "CustomerNotificationService.Workers.dll"]
