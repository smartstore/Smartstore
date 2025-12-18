#nullable enable

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
        /// Performs post-processing operations asynchronously using the specified AI metadata.
        /// </summary>
        /// <param name="localMetadata">The metadata object containing information required for post-processing. Cannot be null.</param>
        /// <returns>Return null if no post-processing was performed. Return an <see cref="AIMetadata"/> instance to update the metadata cache deferredly.</returns>
        Task<AIMetadata?> PostProcessAsync(AIMetadata localMetadata);

        /// <summary>
        /// Replaces the cached metadata for the given <paramref name="moduleSystemName"/> with the given <paramref name="metadata"/> instance.
        /// </summary>
        void ReplaceMetadata(string moduleSystemName, AIMetadata metadata);
    }
}
