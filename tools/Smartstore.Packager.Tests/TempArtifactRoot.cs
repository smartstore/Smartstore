#nullable enable

using System.Text;

namespace Smartstore.Packager.Tests
{
    /// <summary>
    /// Creates a throwaway build artifact directory tree below the system temp directory
    /// and removes it again on dispose.
    /// </summary>
    internal sealed class TempArtifactRoot : IDisposable
    {
        public TempArtifactRoot()
        {
            Root = Path.Combine(Path.GetTempPath(), "Smartstore.Packager.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        /// <summary>
        /// Physical path of the temporary root directory.
        /// </summary>
        public string Root { get; }

        /// <summary>
        /// Physical path of the temporary output directory. Not created upfront.
        /// </summary>
        public string OutputPath => Path.Combine(Root, "_packages");

        /// <summary>
        /// Creates a minimal but complete module directory.
        /// </summary>
        /// <param name="relativePath">Path of the module directory relative to <see cref="Root"/>.</param>
        /// <param name="systemName">The module system name.</param>
        /// <param name="version">The module version.</param>
        public void AddModule(string relativePath, string systemName, string version = "6.0.0")
        {
            var dir = CreateDirectory(relativePath);

            File.WriteAllText(
                Path.Combine(dir, "module.json"),
                $$"""
                {
                  "$schema": "../module.schema.json",
                  "SystemName": "{{systemName}}",
                  "FriendlyName": "{{systemName}}",
                  "Version": "{{version}}",
                  "MinAppVersion": "{{version}}",
                  "Group": "Payment"
                }
                """,
                Encoding.UTF8);

            File.WriteAllText(Path.Combine(dir, $"{systemName}.dll"), "not a real assembly", Encoding.UTF8);
        }

        /// <summary>
        /// Creates a minimal but complete theme directory.
        /// </summary>
        /// <param name="relativePath">Path of the theme directory relative to <see cref="Root"/>.</param>
        /// <param name="themeName">The theme name.</param>
        /// <param name="version">The theme version.</param>
        public void AddTheme(string relativePath, string themeName, string version = "6.0.0")
        {
            var dir = CreateDirectory(relativePath);

            File.WriteAllText(
                Path.Combine(dir, "theme.config"),
                $"""
                <?xml version="1.0" encoding="utf-8"?>
                <Theme title="{themeName}" version="{version}" author="Smartstore AG" />
                """,
                Encoding.UTF8);
        }

        /// <summary>
        /// Creates a directory that contains neither a module nor a theme manifest.
        /// </summary>
        /// <param name="relativePath">Path of the directory relative to <see cref="Root"/>.</param>
        public void AddNonExtensionDirectory(string relativePath)
        {
            var dir = CreateDirectory(relativePath);
            File.WriteAllText(Path.Combine(dir, "readme.txt"), "nothing to see here", Encoding.UTF8);
        }

        private string CreateDirectory(string relativePath)
        {
            var dir = Path.Combine(Root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(dir);
            return dir;
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Root, true);
            }
            catch (IOException)
            {
                // Never let cleanup fail a test run.
            }
        }
    }
}
