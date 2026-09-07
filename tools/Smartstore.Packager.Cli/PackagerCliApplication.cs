#nullable enable

namespace Smartstore.Packager.Cli
{
    /// <summary>
    /// Entry point logic of the packager CLI, decoupled from the process for testability.
    /// </summary>
    public sealed class PackagerCliApplication
    {
        private readonly TextWriter _out;
        private readonly TextWriter _error;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackagerCliApplication"/> class.
        /// </summary>
        /// <param name="output">Writer for regular output.</param>
        /// <param name="error">Writer for error messages and usage hints.</param>
        public PackagerCliApplication(TextWriter output, TextWriter error)
        {
            _out = Guard.NotNull(output);
            _error = Guard.NotNull(error);
        }

        /// <summary>
        /// Parses and executes the given command line.
        /// </summary>
        /// <param name="args">The raw arguments, without the executable name.</param>
        /// <returns>The process exit code, see <see cref="ExitCodes"/>.</returns>
        public async Task<int> RunAsync(string[] args)
        {
            var result = CommandLineParser.Parse(args ?? []);

            if (result.HelpRequested)
            {
                _out.WriteLine(UsageText);
                return ExitCodes.Success;
            }

            if (!result.IsValid)
            {
                _error.WriteLine(result.Error);
                _error.WriteLine();
                _error.WriteLine(UsageText);
                return ExitCodes.InvalidCommandLine;
            }

            try
            {
                return await new PackCommand(_out, _error).RunAsync(result.PackOptions!);
            }
            catch (Exception ex)
            {
                _error.WriteLine(ex.Message);
                return ExitCodes.Error;
            }
        }

        /// <summary>
        /// The usage text printed for <c>--help</c> and for invalid command lines.
        /// </summary>
        public static string UsageText { get; } = string.Join(Environment.NewLine,
            "Smartstore Packager CLI - creates deployable module and theme packages.",
            "",
            "Usage:",
            "  Smartstore.Packager.Cli pack --root <path> --output <path> --extension <name> [--extension <name>...]",
            "  Smartstore.Packager.Cli pack --root <path> --output <path> --all",
            "  Smartstore.Packager.Cli --help",
            "",
            "Commands:",
            "  pack                 Creates a package for each selected module or theme.",
            "",
            "Options:",
            "  --root <path>        Root directory of a build artifact. Either contains 'Modules'",
            "                       and/or 'Themes' subdirectories, or the extension directories",
            "                       themselves. Required.",
            "  --output <path>      Directory the package files are written to. Created if missing.",
            "                       Required.",
            "  --extension <name>   Name of a module or theme to package, matched case-insensitively",
            "                       against its directory name. May be repeated.",
            "  --all                Packages every module and theme found below the root directory.",
            "  -h, --help           Prints this help text.",
            "",
            "Exactly one of '--extension' and '--all' must be specified.",
            "",
            "Exit codes:",
            "  0                    All requested packages were created.",
            "  1                    A runtime, discovery or packaging error occurred.",
            "  2                    The command line was invalid.");
    }
}
