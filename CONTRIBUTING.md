# Contributing

Thanks for helping with the .NET Outbox Pattern. The repo holds the ASP.NET Core reference service and the NuGet library `Zaiets.dotnet.outbox.pattern`.

## Prerequisites

- .NET SDK 10.0.x (pinned in `global.json`, `rollForward: latestMinor`)
- Git
- Docker, optional, for `make docker-up`

Tests use in-memory SQLite, so no database server is needed.

## Build

The root folder holds more than one project or solution file, so pass the solution explicitly. A bare `dotnet build` fails with MSB1011.

```bash
dotnet restore DotnetOutboxPattern.slnx
dotnet build DotnetOutboxPattern.slnx -c Release
```

`DotnetOutboxPattern.slnx` contains the main project `DotnetOutboxPattern.csproj` and the test project `tests/dotnet-outbox-pattern.Tests`. The benchmarks in `dotnet-outbox-pattern.Benchmarks/` build on their own.

## Test

```bash
dotnet test DotnetOutboxPattern.slnx -c Release
```

Filter a single class with `--filter "FullyQualifiedName~OutboxServiceTests"`.

## Coding conventions

- Follow `.editorconfig`: 4 spaces for C#, LF line endings, final newline. Run `dotnet format` before committing.
- `Nullable` and `ImplicitUsings` are enabled, and `Directory.Build.props` sets `TreatWarningsAsErrors`. The build must finish with 0 warnings.
- Every file starts with the `#nullable enable` line and the author header block. Keep it when you add files.
- Public members need XML docs. Guard arguments with `ArgumentNullException.ThrowIfNull` and document the exception.
- Test names follow `MethodName_Condition_ExpectedResult`. Use Arrange-Act-Assert with FluentAssertions.
- Full details are in `CLAUDE.md` and `docs/ARCHITECTURE.md`.

## Branch naming

Branch from `main` with a type prefix and a short kebab-case description:

- `feat/<description>`, for example `feat/circuit-breaker-tostring`
- `fix/<description>`
- `docs/<description>`
- `chore/<description>`

## Commit messages

Use [Conventional Commits](https://www.conventionalcommits.org/). The scope is usually the folder that changed:

```
feat(Infrastructure): add ToString() override to CircuitBreaker
fix(Services): retry dead-letter move on transient failure
docs: document outbox batch claiming
```

Keep each commit to one logical change.

## Pull requests

1. Open the PR against `main`.
2. CI must pass. `ci.yml` restores, builds, and tests the solution. `build.yml` also runs the Docker build, a security scan, and a build with `EnforceCodeStyleInBuild=true`. CodeQL runs on PRs to `main`.
3. Get one approval before merging.
4. Keep the PR focused. Describe what changed, why, and how you tested it.

Report security issues privately as described in [SECURITY.md](SECURITY.md), not in a public issue.
