# Migration Guide: v1.x to v2.0

This document covers breaking changes and the upgrade path from v1.x to v2.0.

## Breaking Changes

### Port Change

The default application port changed from `5000`/`5001` to `8080` (HTTP only).

**Before (v1.x):**
```
ASPNETCORE_URLS=https://+:5001;http://+:5000
EXPOSE 5000 5001
```

**After (v2.0):**
```
ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
```

If you rely on HTTPS at the container level, use a reverse proxy (Caddy, nginx, Traefik) in front of the container.

### Docker Compose

- Removed deprecated `version` field (Compose V2 standard)
- SQL Server healthcheck updated to use `mssql-tools18` path and `-C` flag
- Added `restart: unless-stopped` to all services
- SA password is now configurable via `SA_PASSWORD` environment variable
- Connection string uses `TrustServerCertificate=true` for SQL Server 2022

### Non-root Container User

The runtime container now runs as `appuser` instead of root. If you mount volumes, ensure the host directory has correct permissions:

```bash
# Create logs directory with open permissions before first run
mkdir -p ./logs && chmod 777 ./logs
```

### Health Check

Health check endpoint remains at `/health` but now uses HTTP on port 8080:

```
HEALTHCHECK CMD curl -f http://localhost:8080/health || exit 1
```

## Step-by-step Upgrade

1. **Update `.csproj`** - bump version to `2.0.0`
2. **Replace Dockerfile** - use the new multi-stage build with non-root user
3. **Replace docker-compose.yml** - remove `version` field, update ports
4. **Update port mappings** in any reverse proxy, load balancer, or Kubernetes manifests:
   - Old: `5000` (HTTP) / `5001` (HTTPS)
   - New: `8080` (HTTP)
5. **Update health check URLs** in monitoring tools from `https://...:5001/health` to `http://...:8080/health`
6. **Run database migrations** if upgrading from 0.x:
   ```bash
   dotnet ef database update
   ```
7. **Test locally**:
   ```bash
   docker compose up --build
   curl http://localhost:8080/health
   ```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `SA_PASSWORD` | `YourStrongPassword123!` | SQL Server SA password |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening URL |
| `Outbox__ProcessorEnabled` | `true` | Enable background processor |
| `Outbox__BatchSize` | `100` | Messages per batch |
| `Outbox__MaxRetries` | `5` | Max retry attempts |
| `Outbox__RetryPolicy` | `ExponentialBackoff` | Retry strategy |

## Rollback

If you need to revert to v1.x, restore the previous Dockerfile and docker-compose.yml, and change ports back to 5000/5001.
