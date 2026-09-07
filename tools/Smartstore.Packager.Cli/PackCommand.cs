#nullable enable

using Smartstore.Engine.Modularity;

namespace Smartstore.Packager.Cli
{
    /// <summary>
    /// Executes the <c>pack</c> command: discovers extensions and writes their packages.
    /// </summary>
    public sealed class PackCommand
    {
        private readonly TextWriter _out;
        private readonly TextWriter _error;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackCommand"/> class.
        /// </summary>
        /// <param name="output">Writer for successfully created package paths.</param>
        /// <param name="error">Writer for error messages.</param>
        public PackCommand(TextWriter output, TextWriter error)
        {
            _out = Guard.NotNull(output);
            _error = Guard.NotNull(error);
        }

        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <param name="options">The validated command options.</param>
        /// <returns><see cref="ExitCodes.Success"/> or <see cref="ExitCodes.Error"/>.</returns>
        public async Task<int> RunAsync(PackOptions options)
        {
            Guard.NotNull(options);

            ExtensionScanResult scanResult;

            try
            {
                scanResult = new ExtensionScanner().Scan(options.RootPath);
            }
            catch (Exception ex)
            {
                _error.WriteLine($"Unable to scan '{options.RootPath}': {ex.Message}");
                return ExitCodes.Error;
            }

            var failed = false;

            foreach (var failure in scanResult.Failures)
            {
                _error.WriteLine($"Unable to read extension '{failure.Name}': {failure.Exception.Message}");
                failed = true;
            }

            var selection = Select(options, scanResult, ref failed);

            if (selection.Count == 0)
            {
                if (!failed)
                {
                    _error.WriteLine($"No extensions found in '{options.RootPath}'.");
                }

                return ExitCodes.Error;
            }

            var creator = new PackageCreator(options.RootPath, options.OutputPath);

            foreach (var descriptor in selection)
            {
                try
                {
                    var file = await creator.CreateExtensionPackageAsync(descriptor);
                    _out.WriteLine(file.FullName);
                }
                catch (Exception ex)
                {
                    _error.WriteLine($"Unable to create package for '{descriptor.Name}': {ex.Message}");
                    failed = true;
                }
            }

            return failed ? ExitCodes.Error : ExitCodes.Success;
        }

        private List<IExtensionDescriptor> Select(PackOptions options, ExtensionScanResult scanResult, ref bool failed)
        {
            if (options.All)
            {
                return scanResult.All.ToList();
            }

            var selection = new List<IExtensionDescriptor>();

            foreach (var name in options.ExtensionNames)
            {
                var descriptor = scanResult.Find(name);
                if (descriptor == null)
                {
                    _error.WriteLine($"Unknown extension '{name}'.");
                    failed = true;
                }
                else if (!selection.Contains(descriptor))
                {
                    selection.Add(descriptor);
                }
            }

            return selection;
        }
    }
}
