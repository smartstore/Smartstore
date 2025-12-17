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

        /// <summary>
        /// Asynchronously loads and caches metadata for the given <paramref name="moduleSystemName"/>.
        /// </summary>
        /// <param name="moduleSystemName">The system name of the AI module for which to load metadata. Cannot be null or empty.</param>
        /// <remarks>By default, this method delegates to <see cref="LoadMetadata(string)"/></remarks>
        Task<AIMetadata> LoadMetadataAsync(string moduleSystemName);
    }
}
