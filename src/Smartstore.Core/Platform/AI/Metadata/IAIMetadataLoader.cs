using Smartstore.IO;

namespace Smartstore.Core.AI.Metadata
{
    /// <summary>
    /// Loads and caches AI metadata.
    /// </summary>
    public interface IAIMetadataLoader
    {
        /// <summary>
        /// Loads and caches the metadata from the given root path.
        /// </summary>
        /// <param name="file">metadata.json in the root of a module folder.</param>"
        /// <exception cref="InvalidOperationException"></exception>
        AIMetadata LoadMetadata(IFile file);
    }
}
