#nullable enable

namespace Smartstore.Packager.Cli
{
    /// <summary>
    /// Validated options of the <c>pack</c> command.
    /// </summary>
    public sealed class PackOptions
    {
        /// <summary>
        /// Physical path of the build artifact root directory to scan.
        /// </summary>
        public required string RootPath { get; init; }

        /// <summary>
        /// Physical path of the directory to write package files to.
        /// </summary>
        public required string OutputPath { get; init; }

        /// <summary>
        /// Names of the extensions to package. Empty when <see cref="All"/> is <c>true</c>.
        /// </summary>
        public required IReadOnlyList<string> ExtensionNames { get; init; }

        /// <summary>
        /// Whether all discovered modules and themes should be packaged.
        /// </summary>
        public required bool All { get; init; }
    }
}
