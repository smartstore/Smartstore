namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Responsible for detecting assignments between entities and media files.
    /// </summary>
    public interface IMediaTrackDetector
    {
        /// <summary>
        /// Should return <see langword="true"/> if the implementation class handles 
        /// track detection for given <paramref name="albumName"/>.
        /// </summary>
        bool MatchAlbum(string albumName);

        /// <summary>
        /// Used to announce foreign key properties for media files in any entity type.
        /// This method is called only once on app startup to feed the media tracker database hook
        /// with information about how to react to entity changes (Track, Untrack etc.)
        /// </summary>
        /// <param name="albumName">The name of album to configure tracks for.</param>
        /// <param name="table">The configuration builder.</param>
        void ConfigureTracks(string albumName, TrackedMediaPropertyTable table);

        /// <summary>
        /// Detects all tracks between entities and media files.
        /// </summary>
        /// <param name="albumName">The name of album to detect tracks for.</param>
        IAsyncEnumerable<MediaTrack> DetectAllTracksAsync(string albumName, CancellationToken cancelToken = default);
    }
}