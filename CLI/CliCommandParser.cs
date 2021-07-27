// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetOutboxPattern.CLI;

/// <summary>
/// Parser for command-line arguments
/// Provides structured access to CLI commands and options
/// </summary>
public class CliCommandParser
{
    private readonly Dictionary<string, CliCommand> _commands = new();

    /// <summary>
    /// Registers a CLI command
    /// </summary>
    public void RegisterCommand(CliCommand command)
    {
        _commands[command.Name.ToLower()] = command;
    }

    /// <summary>
    /// Parses command-line arguments and returns the parsed command
    /// </summary>
    public CliCommandContext Parse(string[] args)
    {
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
    /// Gets help text for all commands
    /// </summary>
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
/// Represents a CLI command
/// </summary>
public class CliCommand
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<CliOption> Options { get; set; } = new();
    public Func<CliCommandContext, Task>? Handler { get; set; }
}

/// <summary>
/// Represents a CLI command option
/// </summary>
public class CliOption
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; }
    public string? DefaultValue { get; set; }
}

/// <summary>
/// Context for a parsed CLI command
/// </summary>
public class CliCommandContext
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CommandName { get; set; }
    public CliCommand? Command { get; set; }
    public Dictionary<string, string> Options { get; set; } = new();

    public string GetOption(string name, string? defaultValue = null)
    {
        return Options.TryGetValue(name, out var value) ? value : defaultValue ?? string.Empty;
    }

    public bool HasOption(string name)
    {
        return Options.ContainsKey(name);
    }

    public int GetOptionAsInt(string name, int defaultValue = 0)
    {
        if (GetOption(name) is string value && int.TryParse(value, out var result))
            return result;

        return defaultValue;
    }

    public bool GetOptionAsBoolean(string name, bool defaultValue = false)
    {
        return HasOption(name) || defaultValue;
    }
}
