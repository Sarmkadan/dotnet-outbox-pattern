# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files and restore dependencies (cached layer)
COPY ["DotnetOutboxPattern.csproj", "."]
RUN dotnet restore "DotnetOutboxPattern.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "DotnetOutboxPattern.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DotnetOutboxPattern.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

# Copy published application from publish stage
COPY --from=publish /app/publish .

# Create logs directory with correct permissions
RUN mkdir -p /app/logs && chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
  CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "DotnetOutboxPattern.dll"]
