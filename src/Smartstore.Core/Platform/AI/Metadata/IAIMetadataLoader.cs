namespace Smartstore.Core.AI.Metadata
{
    /// <summary>
    /// Loads and caches AI metadata.
    /// </summary>
    public interface IAIMetadataLoader
    {
        /// <summary>
        /// Loads and caches the metadata for the given <paramref name="moduleSystemName"/>.
        /// </summary>
        /// <param name="moduleSystemName">The AI modules's system name to load metadata.json from.</param>"
        /// <exception cref="InvalidOperationException"></exception>
        AIMetadata LoadMetadata(string moduleSystemName);
    }
}
