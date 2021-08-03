// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Changelog

All notable changes to the Outbox Pattern project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [2.0.0] - 2026-03-17

### Changed
- Default application port changed from 5000/5001 to 8080 (HTTP only)
- Dockerfile rebuilt with multi-stage build, non-root user, and proper HEALTHCHECK
- Docker Compose updated to V2 format (removed deprecated `version` field)
- SQL Server healthcheck updated for mssql-tools18 compatibility
- SA password now configurable via environment variable
- Container runs as non-root `appuser` for improved security
- Added `restart: unless-stopped` policy to all services
- Connection string includes `TrustServerCertificate=true` for SQL Server 2022

### Added
- Migration guide for v1.x to v2.0 (`docs/MIGRATION_v2.md`)
- `.dockerignore` best practices in Dockerfile layer caching

### Security
- Runtime container no longer runs as root
- Reduced attack surface with `--no-install-recommends` in apt-get

## [1.0.0] - 2025-04-07

### Added
- Dead Letter Queue (DLQ) with review and requeue workflow
- Batch export of messages in CSV, JSON, and XML formats
- Comprehensive RabbitMQ and Azure Service Bus publisher examples
- Docker and docker-compose setup for local development
- Integration test suite with in-memory database support
- Message search and filtering API
- Detailed metrics and statistics endpoint (`/api/outbox/statistics`)
- Rate limiting middleware to protect API endpoints
- Kubernetes deployment manifests and liveness probes
- Health check endpoint with detailed component status
- Getting Started guide, Architecture documentation, and FAQ

### Changed
- Split `OutboxService` into separate publishing and query concerns
- Configuration schema now uses strongly-typed options with validation
- Improved error messages with actionable guidance
- Database migrations now use descriptive names
- Tests migrated from NUnit to xUnit

### Fixed
- Race condition in processor lock acquisition under concurrent instances
- Memory leak in background processor during graceful shutdown
- Idempotency key collision detection on high-throughput writes
- Incorrect retry count shown in statistics calculation

### Security
- Input validation added to all API endpoints
- Query parameter length limits enforced
- Rate limiting middleware applied globally
- Error messages sanitized to avoid leaking internal details

## [0.3.0] - 2025-03-17

### Added
- Message ordering based on partition keys (FIFO per logical group)
- Configurable retry policies: Fixed, Linear, and Exponential backoff
- Background processor improvements with configurable batch delays
- Initial performance benchmarks and optimization notes

### Changed
- Processor now batches messages by partition key for correct ordering
- Improved retry delay calculation to include jitter

### Fixed
- Message state transition edge case when lock expired mid-processing
- Database connection pooling exhaustion under sustained load

## [0.2.0] - 2025-02-24

### Added
- `IMessagePublisher` abstraction for pluggable broker implementations
- Default console publisher for local development and testing
- Entity Framework Core integration with SQL Server
- Initial database schema with EF Core migrations
- Strongly-typed `OutboxConfiguration` options class

### Changed
- Simplified configuration structure — single `Outbox` section in appsettings
- Switched from raw ADO.NET to EF Core repository pattern

## [0.1.0] - 2025-02-03

### Added
- Initial project structure targeting .NET 10.0
- Core `OutboxMessage` domain model with state machine
- `IOutboxService` interface and `OutboxService` implementation
- SQL Server data access layer via `OutboxRepository`
- `OutboxProcessor` hosted service for background message polling
- Idempotency key support to prevent duplicate delivery
- Structured logging with Serilog
- Basic REST API for publishing events
- Swagger/OpenAPI documentation
- `appsettings.json` configuration scaffolding
- MIT License

---

## Upgrade Guide

### From 0.x to 1.0

1. Update package references to `1.0.0`
2. Run database migrations: `dotnet ef database update`
3. Update configuration to use the new strongly-typed options
4. Implement a custom `IMessagePublisher` for your message broker
5. Review examples for idempotent subscriber patterns

---

## Support

- Report bugs: https://github.com/sarmkadan/dotnet-outbox-pattern/issues
- Ask questions: https://github.com/sarmkadan/dotnet-outbox-pattern/discussions
- Security issues: security@sarmkadan.com
