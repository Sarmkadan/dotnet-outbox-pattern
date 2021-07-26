# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

# Copy project files
COPY ["DotnetOutboxPattern.csproj", "."]

# Restore dependencies
RUN dotnet restore "DotnetOutboxPattern.csproj"

# Copy source code
COPY . .

# Build application
RUN dotnet build "DotnetOutboxPattern.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "DotnetOutboxPattern.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

# Install curl for health checks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application from publish stage
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Set environment
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=https://+:5001

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=40s --retries=3 \
  CMD curl -f https://localhost:5001/health || exit 1

# Expose ports
EXPOSE 5000 5001

# Run application
ENTRYPOINT ["dotnet", "DotnetOutboxPattern.dll"]
