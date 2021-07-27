// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

# Changelog

All notable changes to the Outbox Pattern project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0] - 2024-02-15

### Added
- Health check endpoint improvements with detailed status reporting
- Configurable alert thresholds for monitoring
- Metrics collection background job with Prometheus integration
- Support for message priority levels in outbox messages
- Bulk requeue operation for dead letters
- Archive operation for published messages with TTL
- Custom serialization hooks for extensibility
- Webhook publisher implementation example
- Kubernetes deployment manifests and HPA configuration
- Azure App Service deployment guide

### Changed
- Optimized database queries for better performance on large tables
- Improved retry policy algorithm with jitter to prevent thundering herd
- Enhanced logging with structured fields for better observability
- Updated Entity Framework Core to 9.0.0
- Refactored processor to batch by partition for better ordering guarantees

### Fixed
- Fix issue where messages could be processed multiple times with certain lock durations
- Prevent DLQ messages from being requeued if processor is disabled
- Handle null partition keys correctly in ordering logic
- Correct timeout calculation in exponential backoff strategy

### Deprecated
- Direct database access patterns (use repositories instead)

## [1.1.0] - 2024-01-20

### Added
- Dead Letter Queue (DLQ) with review workflow
- Requeue functionality for failed messages
- Batch export of messages (CSV, JSON, XML formats)
- Comprehensive examples for RabbitMQ and Kafka integration
- Docker and docker-compose setup for local development
- Integration test suite with in-memory database
- Message search and filtering API
- Detailed metrics and statistics endpoint
- Getting Started guide and Architecture documentation
- Contributing guidelines

### Changed
- Split OutboxService into separate publishing and query concerns
- Improved error messages with actionable guidance
- Configuration schema now uses strongly-typed options
- Database migrations now use descriptive names
- Tests now use xUnit instead of NUnit

### Fixed
- Issue with idempotency key collision detection
- Race condition in processor lock acquisition
- Memory leak in background processor shutdown
- Incorrect retry count in statistics calculation

### Security
- Added input validation on all API endpoints
- Implement query parameter length limits
- Add rate limiting middleware
- Sanitize error messages in responses

## [1.0.0] - 2023-12-01

### Added
- Core outbox pattern implementation
- Support for SQL Server with Entity Framework Core
- Configurable retry policies (Fixed, Linear, Exponential backoff)
- Background processor for message publishing
- IMessagePublisher abstraction for broker integration
- Partition key support for ordered delivery
- Idempotency key support to prevent duplicates
- Health check endpoint
- Structured logging with Serilog
- REST API for publishing events
- Swagger/OpenAPI documentation
- Configuration via appsettings.json
- Comprehensive README and documentation
- MIT License

### Features
- **Guaranteed Delivery**: Messages persisted before publishing
- **Deduplication**: Idempotency keys prevent duplicate processing
- **Ordering**: Partition keys maintain FIFO per logical group
- **Extensible**: IMessagePublisher interface for any broker
- **Observable**: Health checks, metrics, structured logs
- **Configurable**: Retry policies, batch size, processing delays
- **Thread-safe**: Safe for multi-instance deployments

## [0.3.0] - 2023-11-15 (Pre-release)

### Added
- Message ordering based on partition keys
- Configurable retry policies
- Background processor improvements
- Initial performance optimizations

### Fixed
- Issue with message state transitions
- Database connection pooling issues

## [0.2.0] - 2023-10-20 (Pre-release)

### Added
- Basic IMessagePublisher interface
- Default console publisher for testing
- Initial database schema
- Entity Framework Core integration

### Changed
- Simplified configuration structure

## [0.1.0] - 2023-09-15 (Pre-release)

### Added
- Initial project structure
- Core OutboxMessage domain model
- Basic OutboxService interface
- SQL Server data access layer
- Entity Framework Core setup
- Initial documentation

---

## Upgrade Guide

### From 0.x to 1.0

1. Update package references to 1.0.0
2. Run database migrations: `dotnet ef database update`
3. Update configuration to use new strongly-typed options
4. Implement custom IMessagePublisher for your broker
5. Review examples for usage patterns

### From 1.0 to 1.1

1. Update package references to 1.1.0
2. Run database migrations for DLQ support
3. Implement IDeadLetterService in your application
4. Update monitoring to watch DLQ endpoint
5. No breaking changes - fully backward compatible

### From 1.1 to 1.2

1. Update package references to 1.2.0
2. (Optional) Implement health check with new detailed reporting
3. (Optional) Configure alert thresholds for monitoring
4. No database schema changes required
5. Review new monitoring examples
6. Fully backward compatible

---

## Release Schedule

- **v1.3.0** (Q2 2024): Event sourcing integration, CQRS patterns
- **v2.0.0** (Q4 2024): Multi-database support (PostgreSQL, MySQL), Event versioning
- **v3.0.0** (Q2 2025): Distributed transaction support, Saga patterns

---

## Support

- Report bugs: https://github.com/sarmkadan/dotnet-outbox-pattern/issues
- Ask questions: https://github.com/sarmkadan/dotnet-outbox-pattern/discussions
- Security issues: security@sarmkadan.com

