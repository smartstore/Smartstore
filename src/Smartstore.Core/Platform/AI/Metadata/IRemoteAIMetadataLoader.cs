#nullable enable

namespace Smartstore.Core.AI.Metadata;

/// <summary>
/// Loads remote AI metadata from the cloud and maintains a local file cache in App_Data/ai/metadata.
/// </summary>
public interface IRemoteAIMetadataLoader
{
    /// <summary>
    /// Fetches the best available remote metadata for the provider.
    /// Returns cloud metadata when newer than the local metadata, otherwise falls back
    /// to a locally cached copy or the given <paramref name="localMetadata"/>.
    /// </summary>
    /// <param name="localMetadata">The locally installed metadata (from metadata.json). Its version is used for conditional requests.</param>
    /// <param name="cancelToken">Cancellation token.</param>
    /// <returns>
    /// Fresh cloud metadata, cached metadata, or <paramref name="localMetadata"/> if neither is available.
    /// </returns>
    Task<AIMetadata?> FetchAsync(AIMetadata localMetadata, CancellationToken cancelToken = default);
}
