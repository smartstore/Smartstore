#nullable enable

using Smartstore.IconKit.Sources;

namespace Smartstore.IconKit.Cache;

/// <summary>
/// Identifies cache files that still match the current icon sources.
/// </summary>
public interface IIconCacheRecovery
{
    /// <summary>
    /// Checks existing filenames against the supplied catalog and returns the files that are still current.
    /// Does not create or delete files. The lifecycle uses the result to rebuild or validate its index.
    /// </summary>
    IReadOnlyCollection<IconCacheArtifact> GetCurrentFiles(IconCatalog catalog, IReadOnlySet<string> files);
}
