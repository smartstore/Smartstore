#nullable enable

using Smartstore.Core.Theming;
using Smartstore.Engine.Modularity;
using Smartstore.IO;

namespace Smartstore.Packager;

/// <summary>
/// Scans a build artifact directory for packable extensions (modules and themes).
/// </summary>
/// <remarks>
/// Two layouts are supported: a root containing <c>Modules</c> and/or <c>Themes</c>
/// directories, and a root whose direct child directories are the extensions themselves.
/// A directory is recognized as a module by its <c>module.json</c> and as a theme by its
/// <c>theme.config</c> file.
/// </remarks>
public sealed class ExtensionScanner
{
    internal const string ModuleManifestFileName = "module.json";
    internal const string ThemeManifestFileName = "theme.config";

    /// <summary>
    /// Scans the given directory for modules and themes.
    /// </summary>
    /// <param name="rootPath">Physical path of the root directory to scan.</param>
    /// <exception cref="DirectoryNotFoundException">The root directory does not exist.</exception>
    public ExtensionScanResult Scan(string rootPath)
    {
        Guard.NotEmpty(rootPath);

        if (!Directory.Exists(rootPath))
        {
            throw new DirectoryNotFoundException($"The root directory '{rootPath}' does not exist.");
        }

        return Scan(new LocalFileSystem(rootPath));
    }

    /// <summary>
    /// Scans the given file system for modules and themes.
    /// </summary>
    /// <param name="root">The root file system to scan.</param>
    public ExtensionScanResult Scan(IFileSystem root)
    {
        Guard.NotNull(root);

        var modules = new List<IExtensionDescriptor>();
        var themes = new List<IExtensionDescriptor>();
        var failures = new List<ExtensionScanFailure>();

        var dirModules = root.GetDirectory("Modules");
        var dirThemes = root.GetDirectory("Themes");

        var modulesRoot = dirModules.Exists
            ? new LocalFileSystem(dirModules.PhysicalPath)
            : root;

        var themesRoot = dirThemes.Exists
            ? new LocalFileSystem(dirThemes.PhysicalPath)
            : root;

        IEnumerable<IDirectory> dirs = [];

        if (dirModules.Exists || dirThemes.Exists)
        {
            if (dirModules.Exists)
            {
                dirs = dirs.Concat(dirModules.EnumerateDirectories());
            }

            if (dirThemes.Exists)
            {
                dirs = dirs.Concat(dirThemes.EnumerateDirectories());
            }
        }
        else
        {
            dirs = root.EnumerateDirectories("");
        }

        foreach (var dir in dirs)
        {
            bool isTheme = false;

            // Is it a module?
            var filePath = PathUtility.Join(dir.SubPath, ModuleManifestFileName);
            if (!root.FileExists(filePath))
            {
                // ...no! Is it a theme?
                filePath = PathUtility.Join(dir.SubPath, ThemeManifestFileName);
                if (!root.FileExists(filePath))
                    continue;

                isTheme = true;
            }

            try
            {
                if (isTheme)
                {
                    var descriptor = ThemeDescriptor.Create(dir.Name, themesRoot);
                    if (descriptor != null)
                    {
                        themes.Add(descriptor);
                    }
                }
                else
                {
                    var descriptor = ModuleDescriptor.Create(dir, modulesRoot);
                    if (descriptor != null)
                    {
                        modules.Add(descriptor);
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add(new ExtensionScanFailure(dir.Name, ex));
            }
        }

        return new ExtensionScanResult(modules, themes, failures);
    }
}
