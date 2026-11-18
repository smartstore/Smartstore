#nullable enable

namespace Smartstore.IconKit.Sources;

/// <summary>
/// Provides the shared icon catalog and reloads it when its source files change.
/// </summary>
public interface IIconCatalogProvider
{
    /// <summary>
    /// Gets the current catalog. Keep this instance for the whole operation so all lookups use the same source files.
    /// </summary>
    IconCatalog Catalog { get; }
}
