# `CliCommandRegistry`

`CliCommandRegistry` defines the administrative CLI commands available in the library and dispatches parsed command-line arguments to their asynchronous handlers. It is registered as a scoped service by `DependencyInjectionExtensions`.

The constructor requires an `IServiceProvider` and an `ILogger<CliCommandRegistry>`, creates a `CliCommandParser`, and immediately registers every built-in command. Both constructor arguments are checked for `null`. The service provider is retained by the registry, but the current handlers do not use it.

## Command execution

`ExecuteAsync(string[] args)` passes the arguments to `CliCommandParser.Parse`. The first argument selects a command; subsequent tokens beginning with `--` are parsed as options.

- Invalid input prints the parser error and generated help text to standard output, then returns exit code `1`.
- A valid command with a handler awaits that handler and returns exit code `0`.
- A valid command without a handler logs a warning and returns exit code `1`.
- An exception from parsing or a handler is logged, its message is printed to standard output, and exit code `1` is returned.

Command names are matched case-insensitively because the parser lowercases names during both registration and parsing. Option names are stored as written and are therefore case-sensitive when handlers retrieve them.

## Registered commands

| Group | Command | Options | Current handler behavior |
| --- | --- | --- | --- |
| Database | `db-init` | None | Prints initialization progress, waits 100 ms, and prints a success message. The source notes that repository-backed schema initialization is not implemented. |
| Database | `db-migrate` | None | Prints migration progress, waits 100 ms, and prints a success message. |
| Database | `db-seed` | None | Prints sample-data seeding progress, waits 100 ms, and prints a success message. |
| Maintenance | `archive-messages` | `--days <value>`, `--dry-run` | Reads `days` as an integer with a fallback of `30`, treats the presence of `dry-run` as `true`, prints the selected values, waits 100 ms, and prints completion. |
| Maintenance | `cleanup-deadletters` | `--days <value>` | Reads `days` as an integer with a fallback of `90`, prints the selected age, waits 100 ms, and prints completion. |
| Diagnostics | `health-check` | None | Prints fixed messages reporting that the database connection, message processing, and overall system are healthy. It does not query a health service. |
| Diagnostics | `stats` | None | Prints fixed zero counts for pending, processing, published, failed, and dead-letter messages. It does not query a statistics service. |

The `days` fallback is also used when the option is present without a value or contains a value that cannot be parsed as an integer. `--dry-run` is a presence flag: any occurrence makes it `true`, regardless of a following value.

## Extension points

The registry has no public command-registration API. `CliCommandRegistry` is sealed, its `CliCommandParser` is private, and all registration and handler methods are private. Consequently, adding a built-in command requires changing `CliCommandRegistry.cs`:

1. Register a `CliCommand` from `RegisterDatabaseCommands`, `RegisterMaintenanceCommands`, `RegisterDiagnosticsCommands`, or another private method invoked by `RegisterCommands`.
2. Supply its `Name`, `Description`, optional `List<CliOption>`, and a `Func<CliCommandContext, Task>` handler.
3. Read parsed options from the handler's `CliCommandContext` with `GetOption`, `HasOption`, `GetOptionAsInt`, or `GetOptionAsBoolean`.

Handlers can use constructor-injected dependencies. At present, only `IServiceProvider` and `ILogger<CliCommandRegistry>` are injected: the logger is used by `ExecuteAsync`, while the service provider is available to the class but unused by the built-in handlers. Adding new constructor dependencies also requires registering them with the application's dependency-injection container.

For parser-level behavior, `CliCommandParser`, `CliCommand`, `CliOption`, and `CliCommandContext` are public types. They can be used separately to construct another command registry or CLI flow, but they do not provide a way to add commands to an existing `CliCommandRegistry` instance.
