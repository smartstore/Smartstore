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
        /// <param name="rootPath">The app relative root path to the provider folder or directly to metadata.json.</param>"
        /// <exception cref="InvalidOperationException"></exception>
        AIMetadata LoadMetadata(string rootPath);
    }
}
