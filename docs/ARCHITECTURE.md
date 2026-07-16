# Architecture

This document describes how the solution is actually put together - the moving parts, why
they are shaped the way they are, and where the sharp edges live. It is written against the
current code; if the code and this document disagree, the code wins and this file needs a PR.

## What this is

A transactional outbox implementation packaged as an ASP.NET Core application
(`DotnetOutboxPattern.csproj`, net10.0, `Microsoft.NET.Sdk.Web`). It is both a runnable
reference service (Program.cs wires everything up, controllers expose a management API) and
a packable library (`Zaiets.dotnet.outbox.pattern` - the csproj excludes `tests/`,
`examples/` and the benchmarks project from compilation so the package stays clean).

The core promise: domain state and outgoing messages are persisted in the same database
(SQL Server via EF Core), and a background service pushes the messages to a broker
afterwards. If the broker is down, messages wait; if publishing keeps failing, they land in
a dead-letter table for a human.

## Layout

```
Program.cs                  composition root + /health minimal endpoint
Configuration/              DI registration, options types, config builder/presets
Domain/                     OutboxMessage, DeadLetter, enums, domain events, validation
Data/                       OutboxDbContext, OutboxRepository, DeadLetterRepository
Services/                   OutboxService, MessagePublishingService, DeadLetterService,
                            serializer abstraction, metrics/search/export/webhook services
Infrastructure/             OutboxProcessor (BackgroundService), DefaultMessagePublisher,
                            retry/backoff helpers, serialization helper
Controllers/                OutboxMessage / DeadLetter / Metrics / Export / Webhook APIs
Middleware/                 error handling, request logging, rate limiting, perf monitoring
BackgroundServices/         MessageArchivalService, HealthCheckService
Events/, Caching/, CLI/,    supporting "phase 2" services registered by
Integration/, Formatters/     AddOutboxPatternPhase2()
tests/                      xUnit test project
dotnet-outbox-pattern.Benchmarks/  BenchmarkDotNet project
examples/                   standalone usage samples (not compiled into the package)
```

## Core pipeline

### Write side

`IOutboxService` (Services/OutboxService.cs) is the entry point for producers. Overloads
accept a `PublishableEvent` or a `DomainEvent` + topic (+ optional partition key). The
service serializes the payload through the pluggable `IOutboxSerializer` (default:
`SystemTextJsonOutboxSerializer`), builds an `OutboxMessage` in state `Pending`, applies
the idempotency key if one is supplied, and stores it via `IOutboxRepository.AddAsync`.
Idempotency is enforced by a lookup (`GetByIdempotencyKeyAsync`) - publishing twice with
the same key returns the existing message instead of inserting a duplicate.

Because the repository writes through the same `OutboxDbContext`, callers who share that
context get message persistence inside their own business transaction - which is the whole
point of the pattern.

### Read side

`OutboxProcessor` (Infrastructure/OutboxProcessor.cs) is a `BackgroundService` registered
as a hosted service in Program.cs. Its loop, per iteration:

1. Periodically (every `CheckExpiredLocksInterval` ms) releases expired processing locks so
   messages abandoned by a crashed instance become eligible again.
2. Logs a warning if the oldest pending message is older than
   `OldestMessageAgeThresholdMinutes` - a cheap "the pipeline is stuck" signal.
3. Creates a DI scope and calls `IMessagePublishingService.ProcessPendingMessagesAsync`
   with the configured `BatchSize`, then `ProcessScheduledMessagesAsync` for messages with
   a future delivery time.
4. Sleeps. The delay is normally the fixed `DelayBetweenBatches`, but when
   `BackoffStrategy` is `Exponential` and consecutive batches find no work, the delay grows
   by `BackoffMultiplier` per empty batch, capped at `MaxDelayBetweenBatches`, and resets to
   the base value as soon as a batch does work. This keeps an idle deployment from polling
   the database every few seconds forever.

The processor resolves scoped services (`IMessagePublishingService`, which pulls in the
DbContext) from a fresh scope each iteration - the standard way to consume scoped
dependencies from a singleton hosted service.

`MessagePublishingService` (Services/) does the per-message work: lock the row (state
`Processing` + `LockedAt`), call `IMessagePublisher.PublishAsync`, and on success mark
`Published`. On failure it increments the retry count and computes the next attempt time
via the retry policy helpers (fixed / linear / exponential, see `RetryPolicyType` in
Domain/Enums.cs and `RetryPolicyHelper`); once retries are exhausted it creates a
`DeadLetter` via `DeadLetter.FromOutboxMessage(message)` and parks the original.

### Dead letters

`DeadLetterService` + `DeadLetterRepository` + `DeadLetterController` form the manual-review
loop: list unreviewed entries, mark reviewed with notes, or requeue back into the outbox.
The `ReviewRequest`/`RequeueRequest` DTOs live at the bottom of Program.cs.

## Composition (Program.cs)

Two registration layers, deliberately split:

- `AddOutboxPattern(connectionString)` (Configuration/ServiceCollectionExtensions.cs) -
  the minimum viable outbox: `OutboxDbContext` on SQL Server (with
  `EnableRetryOnFailure(3)` and a 30s command timeout), both repositories, the three core
  services, and `TryAdd` registrations for `IOutboxSerializer` and `PublishingOptions` so a
  host can override them before calling in.
- `AddOutboxPatternPhase2()` (Configuration/DependencyInjectionExtensions.cs) - the
  operational extras: metrics (`IMetricsService`, `OutboxMetrics` on a custom
  `DotnetOutboxPattern.Outbox` meter, OpenTelemetry with a Prometheus exporter), webhooks,
  in-memory caching, search, notifications, export formatters (JSON/CSV/XML as multiple
  `IDataFormatter` registrations resolved as `IEnumerable`), a resilient HTTP client
  (registered through `AddHttpClient<ResilientHttpClient>` because it needs a real
  `HttpClient` injected), and the CLI command registry.

The message publisher is swapped via `AddMessagePublisher<T>()`. The shipped
`DefaultMessagePublisher` only logs and simulates latency - it is a stand-in you are
expected to replace with a RabbitMQ/Kafka/Service Bus implementation (see
`examples/02-CustomMessagePublisher.cs`).

Processor settings bind from the `DotnetOutboxPatternOptions.SectionName` config section
with data-annotation validation, and are exposed to the processor through
`IOutboxProcessorOptions` so tests can hand it a stub.

HTTP endpoints come from MVC controllers via `MapControllers()` plus one minimal-API
`/health` route. Historically some routes were registered both ways, which caused
`AmbiguousMatchException` on every request - the minimal-API duplicates were removed and a
comment in Program.cs guards against reintroducing them.

The Phase 2 middleware chain (`UseOutboxPatternMiddleware()`: error handling -> request
logging -> rate limiting -> performance monitoring, in that order on purpose) is applied in
Program.cs before authorization, and the Prometheus scrape endpoint is mapped so the
OpenTelemetry metrics registered in DI are actually reachable.

## Data model

`OutboxDbContext` maps two aggregates:

- **OutboxMessage** - id, aggregate id, topic, event type, serialized payload, state
  (`Pending / Processing / Published / ...` - see `OutboxMessageState`), retry count,
  idempotency key, partition key, correlation id, plus the timestamp trail
  (`CreatedAt`, `PublishedAt`, `LockedAt`, next-retry / scheduled times). Indexed for the
  hot query (state + created-at) and the idempotency lookup.
- **DeadLetter** - snapshot of the failed message plus error, failure count, and the
  review fields (`ReviewedAt`, notes).

The repository is intentionally chatty (`GetByTopicAsync`, `GetByCorrelationIdAsync`,
`GetByDateRangeAsync`, `GetStatisticsAsync`, archival operations...) because the
controllers and the archival/health background services query along all those axes.

## Key design decisions

- **Polling, not CDC/triggers.** A poll loop with batch + lock is boring and portable; SQL
  Server CDC or Debezium would cut latency but drags in infrastructure this repo doesn't
  want to require. The idle backoff strategy is the mitigation for polling's main cost.
- **Locking via state + `LockedAt` timestamp, not `SELECT ... FOR UPDATE`.** Works across
  EF Core without raw SQL, and a crashed worker's lock simply expires
  (`LockDurationSeconds`, default 300s) and gets released by the expired-lock sweep. The
  trade-off: between crash and expiry, a message sits invisible; and delivery is
  at-least-once, never exactly-once - a worker can publish and die before marking
  `Published`. Consumers must be idempotent; the idempotency key gives them the handle.
- **Partition ordering is opt-in** (`PreservePartitionOrdering`). FIFO within a partition
  key costs parallelism, so it is only enforced where the key is set.
- **Interface + concrete options split** (`IOutboxProcessorOptions` vs
  `OutboxProcessorOptions`). The interface keeps the processor testable; backoff settings
  live only on the concrete type, and the processor type-checks for it (`NextDelay()`) so
  custom `IOutboxProcessorOptions` implementations keep the old fixed-delay behaviour
  instead of breaking.
- **Serializer behind `IOutboxSerializer`, registered with `TryAdd`.** System.Text.Json is
  the default; swapping to MessagePack/protobuf is one registration, and hosts that
  register their own before `AddOutboxPattern` win.
- **SQL Server is hardcoded in `AddOutboxPattern`.** Deliberate simplification for the
  reference implementation; supporting other providers would mean either an options
  callback or provider-specific packages. Known limitation, see below.

## Extension points

- `IMessagePublisher` - the broker adapter. The one you will always implement.
- `IOutboxSerializer` - payload serialization.
- `IDataFormatter` (+ `AddDataFormatter<T>()`) - additional export formats.
- `IOutboxProcessorOptions` - alternative options sources for the processor.
- Decorating `IOutboxService` - enrichment (correlation ids, user context) without touching
  the pipeline.
- `ConfigureRateLimiting` / `ConfigureMessageArchival` / `ConfigureHealthCheck` - tuning
  knobs for the phase 2 services.

## Known limitations

- `DefaultMessagePublisher` does not talk to any real broker; the app "works" out of the
  box but delivers nothing anywhere.
- EF provider is fixed to SQL Server inside `AddOutboxPattern`.
- `MessageArchivalService` and `HealthCheckService` exist and have `Configure*` helpers,
  but are not registered as hosted services in Program.cs - archival must be wired
  explicitly by the host.
- `AddExternalHttpClients` builds its own `LoggerFactory` and materializes clients at
  registration time - fine for a handful of static clients, wrong if you need per-request
  configuration.
- At-least-once delivery only; exactly-once requires idempotent consumers (see above).
- `Services/OutboxService.cs.backup*` files are editor leftovers, not part of the build.

## Testing

`tests/dotnet-outbox-pattern.Tests` (xUnit) covers domain validation, serialization,
retry-policy math, services and repositories; `dotnet-outbox-pattern.Benchmarks` holds
BenchmarkDotNet suites for the repository, serializer and publishing service. Both are
excluded from the NuGet package build.
