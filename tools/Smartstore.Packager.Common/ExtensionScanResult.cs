#nullable enable

using Smartstore.Engine.Modularity;

namespace Smartstore.Packager;

/// <summary>
/// Describes an extension directory that could not be materialized into a descriptor.
/// </summary>
/// <param name="Name">Name of the extension directory.</param>
/// <param name="Exception">The exception that occurred while reading the manifest.</param>
public sealed record ExtensionScanFailure(string Name, Exception Exception);

/// <summary>
/// The result of scanning a directory for packable extensions.
/// </summary>
public sealed class ExtensionScanResult
{
    internal ExtensionScanResult(
        IReadOnlyList<IExtensionDescriptor> modules,
        IReadOnlyList<IExtensionDescriptor> themes,
        IReadOnlyList<ExtensionScanFailure> failures)
    {
        Modules = modules;
        Themes = themes;
        Failures = failures;
    }

    /// <summary>
    /// All modules found, in directory order.
    /// </summary>
    public IReadOnlyList<IExtensionDescriptor> Modules { get; }

    /// <summary>
    /// All themes found, in directory order.
    /// </summary>
    public IReadOnlyList<IExtensionDescriptor> Themes { get; }

    /// <summary>
    /// Extension directories whose manifest could not be read.
    /// </summary>
    public IReadOnlyList<ExtensionScanFailure> Failures { get; }

    /// <summary>
    /// All modules and themes found.
    /// </summary>
    public IEnumerable<IExtensionDescriptor> All => Modules.Concat(Themes);

    /// <summary>
    /// Finds a module or theme by its (system) name, ignoring case.
    /// </summary>
    /// <param name="name">The extension name, which equals its directory name.</param>
    /// <returns>The descriptor, or <c>null</c> if no extension matches.</returns>
    public IExtensionDescriptor? Find(string name)
    {
        Guard.NotEmpty(name);

        return All.FirstOrDefault(x => x.Name.EqualsNoCase(name));
    }
}
