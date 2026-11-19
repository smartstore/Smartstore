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
    /// <param name="catalog">The current libraries, variants and kit definitions used to check file revisions.</param>
    /// <param name="files">Paths of existing files relative to the icon cache folder, including current and older revisions.</param>
    /// <param name="knownFiles">Entries from the saved index that link files to their kit, library and variant.
    /// These selections are checked directly. Files without an entry require a search across all selections.
    /// Pass null when no usable index exists.</param>
    IReadOnlyCollection<IconCacheArtifact> GetCurrentFiles(
        IconCatalog catalog,
        IReadOnlySet<string> files,
        IReadOnlyCollection<IconCacheArtifact>? knownFiles = null);
}
