# CLAUDE.md

Transactional outbox pattern for .NET 10: an ASP.NET Core reference service (EF Core + SQL Server, background publisher, dead-letter loop) that is also packed as the NuGet library `Zaiets.dotnet.outbox.pattern`.

## Build

```bash
dotnet restore
dotnet build -c Release          # or: make build
dotnet run -c Release            # or: make run  (API at https://localhost:5001, /health)
dotnet pack -c Release -o nupkg  # or: make pack
```

- SDK pinned in `global.json` (10.0.100, rollForward latestMinor).
- Solution file: `DotnetOutboxPattern.slnx` (main project + tests). The benchmarks project is not in the solution; build it separately from `dotnet-outbox-pattern.Benchmarks/`.
- `tests/`, `examples/` and the benchmarks folder are excluded from the main csproj via `Compile Remove`.
- Docker: `make docker-build`, `make docker-up` (`docker-compose.yml`).

## Test

```bash
dotnet test -c Release                       # or: make test
dotnet test --filter "FullyQualifiedName~OutboxServiceTests"
```

- Test project: `tests/dotnet-outbox-pattern.Tests/` - xUnit 2.9, FluentAssertions 7, Moq, `Microsoft.AspNetCore.Mvc.Testing`.
- Integration tests use in-memory SQLite (`Microsoft.EntityFrameworkCore.Sqlite`), no external DB needed.
- CI (`.github/workflows/build.yml`, `ci.yml`): restore, build Release, test `--no-build`, Docker build, CodeQL.

## Lint / Format

```bash
dotnet format                                              # or: make format
dotnet build -c Release -p:EnforceCodeStyleInBuild=true    # CI style check
```

- Rules live in `.editorconfig` (4 spaces for C#, 2 for csproj/json/yml, LF, final newline).
- `Directory.Build.props`: `Nullable` and `ImplicitUsings` enabled, `LangVersion latest`, warnings are not errors, `GenerateDocumentationFile` on (missing XML docs produce warnings).

## Key directories and entry points

| Path | Purpose |
| --- | --- |
| `Program.cs` | Composition root, Serilog setup, `/health` minimal endpoint, `MapControllers()` |
| `Configuration/` | `AddOutboxPattern()` (core DI), `AddOutboxPatternPhase2()` (metrics, webhooks, cache, export, CLI), options types |
| `Domain/` | `OutboxMessage`, `DeadLetter`, enums, domain events, validation |
| `Data/` | `OutboxDbContext`, `OutboxRepository`, `DeadLetterRepository` |
| `Services/` | `OutboxService` (write side), `MessagePublishingService` (per-message publish/retry), `DeadLetterService`, serializer, metrics/search/export/webhook |
| `Infrastructure/` | `OutboxProcessor` (BackgroundService polling loop), `DefaultMessagePublisher` (stub, replace it), `RetryPolicyHelper`, `BackoffMath`, `CircuitBreaker` |
| `Controllers/` | Management API: outbox messages, dead letters, metrics, export, webhooks |
| `Middleware/` | Error handling, request logging, rate limiting, perf monitoring |
| `BackgroundServices/` | `MessageArchivalService`, `HealthCheckService` |
| `Events/`, `Caching/`, `CLI/`, `Integration/`, `Formatters/` | Phase-2 extras |
| `examples/` | Standalone usage samples (not compiled) |
| `docs/ARCHITECTURE.md` | Authoritative design description; update it when behaviour changes |

Core flow: `IOutboxService.PublishEventAsync` -> row in `Pending` state (idempotency key dedup) -> `OutboxProcessor` claims a batch (`Processing` + `LockedAt`) -> `IMessagePublisher.PublishAsync` -> `Published`, or retry with fixed/linear/exponential backoff, then `DeadLetter` after max attempts.

## Conventions

- Naming: `PascalCase` types/members, `_camelCase` private fields, `camelCase` locals, `UPPER_CASE` constants. Namespaces mirror folders under `DotnetOutboxPattern.*`.
- Every file starts with `#nullable enable` and the author header comment block; keep it when adding files.
- Public methods guard arguments with `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrEmpty` and document them with `/// <exception>`. XML docs on all public types and members.
- One public class per file, aim under 500 lines. Extension helpers go in `*Extensions.cs` next to the type.
- Options are bound via `IOptions<T>` with data-annotation validation; expose them to services through interfaces (e.g. `IOutboxProcessorOptions`) so tests can stub them.
- Scoped services (DbContext, publishing service) are resolved from a fresh `IServiceScope` inside singleton hosted services.
- Tests: `MethodName_Condition_ExpectedResult`, Arrange-Act-Assert, FluentAssertions, `[Theory]`/`[InlineData]` for tabular cases, `sealed` test classes.
- Commits follow Conventional Commits (`feat(Infrastructure): ...`, `docs: ...`, `chore: ...`).
