#nullable enable

namespace Smartstore.IconKit.Sources;

/// <summary>
/// Finds the library icon for a name, using mappings and configured fallbacks.
/// </summary>
public interface IIconResolver
{
    /// <summary>
    /// Resolves an address within the supplied catalog, or returns null if no matching icon exists.
    /// </summary>
    /// <param name="catalog">The catalog used for this operation.</param>
    /// <param name="address">The icon name and preferred library and variant. SkipMapping selects the source name directly.</param>
    /// <param name="indexOnly">Use the name index without reading individual source files. Used when planning whole kits.</param>
    IconInfo? Resolve(IconCatalog catalog, IconAddress address, bool indexOnly = false);
}
