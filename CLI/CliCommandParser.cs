#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetOutboxPattern.Utilities;

namespace DotnetOutboxPattern.CLI;

/// <summary>
/// Parses command-line arguments and provides structured access to registered commands and options.
/// </summary>
public sealed class CliCommandParser
{
    private readonly Dictionary<string, CliCommand> _commands = new();

    /// <summary>
    /// Registers a CLI command, replacing any command with the same case-insensitive name.
    /// </summary>
    /// <param name="command">The command to register.</param>
    public void RegisterCommand(CliCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _commands[command.Name.ToLower()] = command;
    }

    /// <summary>
    /// Parses command-line arguments and resolves the requested command and its options.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <returns>A context describing the resolved command, options, and validation result.</returns>
    public CliCommandContext Parse(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.Length == 0)
            return new CliCommandContext { IsValid = false, ErrorMessage = "No command provided" };

        var commandName = args[0].ToLower();

        if (!_commands.TryGetValue(commandName, out var command))
            return new CliCommandContext
            {
                IsValid = false,
                ErrorMessage = $"Unknown command: {commandName}"
            };

        var options = ParseOptions(args.Skip(1).ToArray());

        return new CliCommandContext
        {
            IsValid = true,
            CommandName = commandName,
            Command = command,
            Options = options
        };
    }

    /// <summary>
    /// Gets help text for all registered commands and their options.
    /// </summary>
    /// <returns>The formatted help text.</returns>
    public string GetHelpText()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Available Commands:");
        sb.AppendLine();

        foreach (var command in _commands.Values)
        {
            sb.AppendLine($"  {command.Name,-20} {command.Description}");

            if (!command.Options.IsNullOrEmpty())
            {
                foreach (var option in command.Options)
                {
                    sb.AppendLine($"    --{option.Name,-18} {option.Description}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private Dictionary<string, string> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, string>();

        for (int i = 0; i < args.Length; i++)
        {
            if (args[i].StartsWith("--"))
            {
                var key = args[i].Substring(2);
                var value = string.Empty;

                if (i + 1 < args.Length && !args[i + 1].StartsWith("--"))
                {
                    value = args[i + 1];
                    i++;
                }

                options[key] = value;
            }
        }

        return options;
    }
}

/// <summary>
/// Represents a CLI command and its executable handler.
/// </summary>
public sealed class CliCommand
{
    /// <summary>
    /// Gets or sets the command name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the command description shown in help text.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the options supported by the command.
    /// </summary>
    public List<CliOption> Options { get; set; } = new();

    /// <summary>
    /// Gets or sets the asynchronous handler invoked for the command.
    /// </summary>
    public Func<CliCommandContext, Task>? Handler { get; set; }
}

/// <summary>
/// Represents an option accepted by a CLI command.
/// </summary>
public sealed class CliOption
{
    /// <summary>
    /// Gets or sets the option name without the leading double hyphen.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the option description shown in help text.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the option is required.
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Gets or sets the default option value.
    /// </summary>
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Provides the result and resolved values for a parsed CLI command.
/// </summary>
public sealed class CliCommandContext
{
    /// <summary>
    /// Gets or sets a value indicating whether the command-line arguments are valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation error message, if any.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Gets or sets the normalized name of the resolved command.
    /// </summary>
    public string? CommandName { get; set; }

    /// <summary>
    /// Gets or sets the resolved command.
    /// </summary>
    public CliCommand? Command { get; set; }

    /// <summary>
    /// Gets or sets the parsed option values keyed by option name.
    /// </summary>
    public Dictionary<string, string> Options { get; set; } = new();

    /// <summary>
    /// Gets the value of an option or a fallback value when the option is absent.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="defaultValue">The fallback value to return when the option is absent.</param>
    /// <returns>The option value, the supplied fallback value, or an empty string.</returns>
    public string GetOption(string name, string? defaultValue = null)
    {
        return Options.TryGetValue(name, out var value) ? value : defaultValue ?? string.Empty;
    }

    /// <summary>
    /// Determines whether an option was provided.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <returns><see langword="true"/> when the option is present; otherwise, <see langword="false"/>.</returns>
    public bool HasOption(string name)
    {
        return Options.ContainsKey(name);
    }

    /// <summary>
    /// Gets an option value as an integer.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="defaultValue">The value to return when the option is absent or is not a valid integer.</param>
    /// <returns>The parsed integer value, or <paramref name="defaultValue"/> when parsing fails.</returns>
    public int GetOptionAsInt(string name, int defaultValue = 0)
    {
        if (GetOption(name) is string value && int.TryParse(value, out var result))
            return result;

        return defaultValue;
    }

    /// <summary>
    /// Gets whether an option is present, falling back to a default value when it is absent.
    /// </summary>
    /// <param name="name">The option name.</param>
    /// <param name="defaultValue">The value to return when the option is absent.</param>
    /// <returns><see langword="true"/> when the option is present; otherwise, <paramref name="defaultValue"/>.</returns>
    public bool GetOptionAsBoolean(string name, bool defaultValue = false)
    {
        return HasOption(name) || defaultValue;
    }
}
