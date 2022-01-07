using Smartstore.Core.Configuration;

namespace Smartstore.Core.Content.Media
{
    public interface IMediaTracker
    {
        IDisposable BeginScope(bool makeFilesTransientWhenOrphaned);

        Task TrackAsync(BaseEntity entity, int mediaFileId, string propertyName);
        Task UntrackAsync(BaseEntity entity, int mediaFileId, string propertyName);

        Task TrackAsync<TSetting>(TSetting settings, int? prevMediaFileId, Expression<Func<TSetting, int>> path) where TSetting : ISettings, new();
        Task TrackAsync<TSetting>(TSetting settings, int? prevMediaFileId, Expression<Func<TSetting, int?>> path) where TSetting : ISettings, new();

        Task TrackManyAsync(IEnumerable<MediaTrack> tracks);
        Task TrackManyAsync(string albumName, IEnumerable<MediaTrack> tracks);

        Task<int> DeleteAllTracksAsync(string albumName);
        Task DetectAllTracksAsync(string albumName, CancellationToken cancelToken = default);
        bool TryGetTrackedPropertiesFor(Type forType, out IEnumerable<TrackedMediaProperty> properties);
    }
}
