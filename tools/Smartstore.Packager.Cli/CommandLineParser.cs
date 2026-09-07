#nullable enable

namespace Smartstore.Packager.Cli;

/// <summary>
/// The outcome of parsing the command line.
/// </summary>
public sealed class CommandLineParseResult
{
    private CommandLineParseResult()
    {
    }

    /// <summary>
    /// The parsed options of the <c>pack</c> command, or <c>null</c>.
    /// </summary>
    public PackOptions? PackOptions { get; private init; }

    /// <summary>
    /// Whether the user asked for the help text.
    /// </summary>
    public bool HelpRequested { get; private init; }

    /// <summary>
    /// The reason why the command line was rejected, or <c>null</c>.
    /// </summary>
    public string? Error { get; private init; }

    /// <summary>
    /// Whether the command line was accepted.
    /// </summary>
    public bool IsValid => Error == null;

    internal static CommandLineParseResult Pack(PackOptions options)
        => new() { PackOptions = options };

    internal static CommandLineParseResult Help()
        => new() { HelpRequested = true };

    internal static CommandLineParseResult Fail(string error)
        => new() { Error = error };
}

/// <summary>
/// A minimal parser for the small command surface of the packager CLI.
/// </summary>
public static class CommandLineParser
{
    /// <summary>
    /// Parses the given command line arguments.
    /// </summary>
    /// <param name="args">The raw arguments, without the executable name.</param>
    public static CommandLineParseResult Parse(IReadOnlyList<string> args)
    {
        Guard.NotNull(args);

        if (args.Count == 0)
        {
            return CommandLineParseResult.Fail("No command specified.");
        }

        if (args[0].EqualsNoCase("help") || args.Any(IsHelpSwitch))
        {
            return CommandLineParseResult.Help();
        }

        var command = args[0];
        if (!command.EqualsNoCase("pack"))
        {
            return CommandLineParseResult.Fail($"Unknown command '{command}'.");
        }

        return ParsePack(args);
    }

    private static CommandLineParseResult ParsePack(IReadOnlyList<string> args)
    {
        string? rootPath = null;
        string? outputPath = null;
        var extensionNames = new List<string>();
        var all = false;

        for (var i = 1; i < args.Count; i++)
        {
            var (name, inlineValue) = SplitArgument(args[i]);

            switch (name.ToLowerInvariant())
            {
                case "--all":
                    if (inlineValue != null)
                    {
                        return CommandLineParseResult.Fail("Option '--all' does not take a value.");
                    }
                    all = true;
                    break;
                case "--root":
                case "--output":
                case "--extension":
                    if (!TryReadValue(args, ref i, name, inlineValue, out var value, out var error))
                    {
                        return CommandLineParseResult.Fail(error);
                    }

                    if (name.EqualsNoCase("--root"))
                    {
                        rootPath = value;
                    }
                    else if (name.EqualsNoCase("--output"))
                    {
                        outputPath = value;
                    }
                    else
                    {
                        extensionNames.Add(value);
                    }
                    break;
                default:
                    return CommandLineParseResult.Fail($"Unknown option '{args[i]}'.");
            }
        }

        if (rootPath.IsEmpty())
        {
            return CommandLineParseResult.Fail("Option '--root' is required.");
        }

        if (outputPath.IsEmpty())
        {
            return CommandLineParseResult.Fail("Option '--output' is required.");
        }

        if (all && extensionNames.Count > 0)
        {
            return CommandLineParseResult.Fail("Options '--all' and '--extension' are mutually exclusive.");
        }

        if (!all && extensionNames.Count == 0)
        {
            return CommandLineParseResult.Fail("Either '--all' or at least one '--extension' must be specified.");
        }

        return CommandLineParseResult.Pack(new PackOptions
        {
            RootPath = rootPath,
            OutputPath = outputPath,
            ExtensionNames = extensionNames,
            All = all
        });
    }

    private static bool TryReadValue(
        IReadOnlyList<string> args,
        ref int index,
        string name,
        string? inlineValue,
        out string value,
        out string error)
    {
        if (inlineValue == null && index + 1 < args.Count && !args[index + 1].StartsWith('-'))
        {
            inlineValue = args[++index];
        }

        value = inlineValue ?? string.Empty;

        if (value.IsEmpty())
        {
            error = $"Option '{name}' requires a value.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static (string Name, string? Value) SplitArgument(string arg)
    {
        var index = arg.IndexOf('=');
        return index < 0
            ? (arg, null)
            : (arg[..index], arg[(index + 1)..]);
    }

    private static bool IsHelpSwitch(string arg)
        => arg.EqualsNoCase("--help") || arg.EqualsNoCase("-h") || arg.EqualsNoCase("-?");
}
