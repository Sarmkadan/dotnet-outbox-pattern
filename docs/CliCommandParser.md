# CliCommandParser

Provides a lightweight infrastructure for defining, registering, and parsing command‑line interface (CLI) commands within the *dotnet-outbox-pattern* project. The type aggregates command metadata (name, description, options, handler) and offers methods to register the command, parse input arguments, retrieve help text, and inspect the results of parsing.

## API

### RegisterCommand
```csharp
public void RegisterCommand()
```
Registers the command described by the instance’s properties (`Name`, `Description`, `Options`, `Handler`) with the underlying parser infrastructure.  
- **Parameters:** None.  
- **Return value:** None (void).  
- **Exceptions:**  
  - `InvalidOperationException` – if `Name` is null or empty, or if `Handler` is null when a command is expected to be executable.  
  - `ArgumentNullException` – if `Options` is set to null before registration.

### Parse
```csharp
public CliCommandContext Parse()
```
Parses the command line arguments (taken from the process’ startup arguments or a previously supplied source) into a `CliCommandContext` that reflects the registered command.  
- **Parameters:** None.  
- **Return value:** A populated `CliCommandContext` instance representing the parsed command, its options, and any remaining operands.  
- **Exceptions:**  
  - `FormatException` – if the arguments do not conform to the expected syntax (e.g., missing required option, malformed value).  
  - `InvalidOperationException` – if `RegisterCommand` has not been called prior to invoking this method.

### GetHelpText
```csharp
public string GetHelpText()
```
Returns a formatted help string for the registered command, suitable for display to users (e.g., in response to `--help`).  
- **Parameters:** None.  
- **Return value:** A multi‑line string containing the command name, description, usage pattern, and option descriptions; returns `null` if the command has not been registered.  
- **Exceptions:** None.

### Name (first)
```csharp
public string Name { get; set; }
```
Gets or sets the identifier of the command (the token used on the command line to invoke it).  
- **Parameters:** None.  
- **Return value:** The command name.  
- **Exceptions:**  
  - `ArgumentNullException` – when setting to `null`.  
  - `ArgumentException` – when setting to an empty string or whitespace.

### Description (first)
```csharp
public string Description { get; set; }
```
Gets or sets the explanatory text shown in help output for the command.  
- **Parameters:** None.  
- **Return value:** The command description.  
- **Exceptions:**  
  - `ArgumentNullException` – when setting to `null`.

### Options (first)
```csharp
public List<CliOption> Options { get; set; }
```
Gets or sets the collection of option definitions that the command accepts. Each `CliOption` describes a flag or valued parameter.  
- **Parameters:** None.  
- **Return value:** A mutable list of `CliOption` objects.  
- **Exceptions:**  
  - `ArgumentNullException` – when setting to `null`.

### Handler
```csharp
public Func<CliCommandContext, Task>? Handler { get; set; }
```
Gets or sets the asynchronous delegate executed when the command is invoked after successful parsing.  
- **Parameters:** None.  
- **Return value:** A `Func<CliCommandContext, Task>` or `null` if no handler is assigned.  
- **Exceptions:** None.

### Name (second)
```csharp
public string Name { get; }
```
Gets the name of the command that was most recently parsed. This property is read‑only after a successful call to `Parse`.  
- **Parameters:** None.  
- **Return value:** The parsed command name, or `null` if no command has been parsed yet.  
- **Exceptions:**  
  - `InvalidOperationException` – accessed before any parsing operation has succeeded.

### Description (second)
```csharp
public string Description { get; }
```
Gets the description of the command that was most recently parsed.  
- **Parameters:** None.  
- **Return value:** The parsed command description, or `null` if no command has been parsed yet.  
- **Exceptions:**  
  - `InvalidOperationException` – accessed before any parsing operation has succeeded.

### IsRequired
```csharp
public bool IsRequired { get; set; }
```
Indicates whether the option currently under consideration (as reflected by the parser’s internal state) must be supplied by the user.  
- **Parameters:** None.  
- **Return value:** `true` if the option is mandatory; otherwise `false`.  
- **Exceptions:** None.

### DefaultValue
```csharp
public string? DefaultValue { get; set; }
```
Gets or sets the value to use for an option when the user does not provide one.  
- **Parameters:** None.  
- **Return value:** The default value string, or `null` if no default is defined.  
- **Exceptions:** None.

### IsValid
```csharp
public bool IsValid { get; }
```
Gets a flag indicating whether the most recent parsing operation succeeded without errors.  
- **Parameters:** None.  
- **Return value:** `true` if the parsed `CliCommandContext` is valid; otherwise `false`.  
- **Exceptions:** None.

### ErrorMessage
```csharp
public string? ErrorMessage { get; }
```
Gets the explanatory message associated with a failed parsing operation, if any.  
- **Parameters:** None.  
- **Return value:** An error message string, or `null` when `IsValid` is `true`.  
- **Exceptions:** None.

### CommandName
```csharp
public string? CommandName { get; set; }
```
Gets or sets the name of the command identified during parsing.  
- **Parameters:** None.  
- **Return value:** The command name extracted from the input, or `null` if not yet parsed.  
- **Exceptions:** None.

### Command
```csharp
public CliCommand? Command { get; set; }
```
Gets or sets the parsed `CliCommand` object that encapsulates the command and its options after a successful parse.  
- **Parameters:** None.  
- **Return value:** A `CliCommand` instance, or `null` if parsing has not occurred or failed.  
- **Exceptions:** None.

### Options (second)
```csharp
public Dictionary<string, string> Options { get; }
```
Gets a read‑only dictionary mapping option names to their string values as obtained from the last successful parse.  
- **Parameters:** None.  
- **Return value:** A dictionary where the key is the option identifier (e.g., `"verbose"` or `"output"`) and the value is the user‑supplied argument; empty if no options were present.  
- **Exceptions:** None.

### GetOption
```csharp
public string GetOption()
```
Retrieves the string value of the option that was last accessed via the parser’s internal cursor (typically the most recently queried option).  
- **Parameters:** None.  
- **Return value:** The option’s value as a string.  
- **Exceptions:**  
  - `InvalidOperationException` – if no option has been selected or the parser has not been invoked.  
  - `KeyNotFoundException` – if the selected option is not present in the parsed options dictionary.

### HasOption
```csharp
public bool HasOption()
```
```csharp
public bool HasOption()
```
Determines whether the option last selected by the parser’s internal cursor exists in the parsed options collection.  
- **Parameters:** None.  
- **Return value:** `true` if the option is present; otherwise `false`.  
- **Exceptions:**  
  - `InvalidOperationException` – if the parser has not been invoked or no option has been selected.

### GetOptionAsInt
```csharp
public int GetOptionAsInt()
```
Attempts to convert the value of the last selected option to a 32‑bit signed integer.  
- **Parameters:** None.  
- **Return value:** The integer representation of the option’s value.  
- **Exceptions:**  
  - `FormatException` – if the option’s value cannot be parsed as an integer.  
  - `InvalidOperationException` – if no option has been selected or the parser has not been invoked.

### GetOptionAsBoolean
```csharp
public bool GetOptionAsBoolean()
```
Attempts to interpret the value of the last selected option as a Boolean (`true`/`false`). Accepts case‑insensitive strings `"true"`, `"false"`, `"1"`, `"0"`; other values cause an exception.  
- **Parameters:** None.  
- **Return value:** The Boolean value of the option.  
- **Exceptions:**  
  - `FormatException` – if the option’s value is not a recognizable Boolean representation.  
  - `InvalidOperationException` – if no option has been selected or the parser has not been invoked.

## Usage

### Defining and registering a simple command
```csharp
using System.Threading.Tasks;
using DotnetOutboxPattern.Cli; // assumed namespace

var parser = new CliCommandParser
{
    Name = "process",
    Description = "Processes pending outbox messages.",
    Options = new List<CliOption>
    {
        new CliOption { Name = "batch", IsRequired = false, DefaultValue = "100" },
        new CliOption { Name = "verbose", IsRequired = false }
    },
    Handler = async ctx =>
    {
        var batchSize = int.Parse(ctx.GetOption("batch") ?? "100");
        var verbose = bool.Parse(ctx.GetOption("verbose") ?? "false");
        // processing logic...
        await Task.CompletedTask;
    }
};

parser.RegisterCommand(); // makes the command available to the parser
```

### Parsing input and reacting to the result
```csharp
var ctx = parser.Parse(); // uses Environment.GetCommandLineArgs() internally

if (!ctx.IsValid)
{
    Console.Error.WriteLine($"Error: {ctx.ErrorMessage}");
    return 1;
}

if (ctx.CommandName == "process")
{
    await parser.Handler!(ctx); // safe‑invoke the registered handler
    return 0;
}

Console.WriteLine(parser.GetHelpText());
return 0;
```

## Notes

- The parser assumes that `RegisterCommand` is called exactly once per `CliCommandParser` instance before any call to `Parse`. Re‑registering after parsing may lead to undefined behavior.  
- All state‑dependent members (`Name`, `Description`, `IsValid`, `ErrorMessage`, `CommandName`, `Command`, `Options`, `GetOption*`, `HasOption`) reflect the outcome of the most recent `Parse` call; their values are stale if `Parse` is not re‑invoked after changing command line input.  
- The type is **not thread‑safe**. Concurrent calls to `Parse` or mutation of properties such as `Options` or `Handler` from multiple threads may result in race conditions. External synchronization is required if the instance is shared.  
- `GetOption`, `HasOption`, `GetOptionAsInt`, and `GetOptionAsBoolean` operate on an internal cursor that tracks the last option queried via the parser’s option‑lookup helpers. If these methods are called without a prior option selection (e.g., directly after `Parse`), they will throw `InvalidOperationException`.  
- Nullable reference types are respected: `DefaultValue`, `ErrorMessage`, `CommandName`, and `Command` may legitimately be `null`. Consumers should check for null before dereferencing.  
- The `Handler` property may be set to `null` to define a command that only provides help or performs validation without side effects; invoking a null handler will result in a `NullReferenceException` unless guarded.
