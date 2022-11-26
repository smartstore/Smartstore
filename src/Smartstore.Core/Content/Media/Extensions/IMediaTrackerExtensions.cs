using System.Runtime.CompilerServices;

namespace Smartstore.Core.Content.Media
{
    public static class IMediaTrackerExtensions
    {
        public static Task TrackAsync<T>(this IMediaTracker tracker, T entity, Expression<Func<T, int>> path) where T : BaseEntity, new()
        {
            Guard.NotNull(path, nameof(path));

            var getter = path.GetPropertyInvoker();
            var mediaFileId = getter.Invoke(entity);
            if (mediaFileId > 0)
                return tracker.TrackAsync(entity, mediaFileId, path.ExtractPropertyInfo().Name);

            return Task.CompletedTask;
        }

        public static Task TrackAsync<T>(this IMediaTracker tracker, T entity, Expression<Func<T, int?>> path) where T : BaseEntity, new()
        {
            Guard.NotNull(path, nameof(path));

            var getter = path.GetPropertyInvoker();
            var mediaFileId = getter.Invoke(entity);
            if (mediaFileId > 0)
                return tracker.TrackAsync(entity, mediaFileId.Value, path.ExtractPropertyInfo().Name);

            return Task.CompletedTask;
        }

        public static Task UntrackAsync<T>(this IMediaTracker tracker, T entity, Expression<Func<T, int>> path) where T : BaseEntity, new()
        {
            Guard.NotNull(path, nameof(path));

            var getter = path.GetPropertyInvoker();
            var mediaFileId = getter.Invoke(entity);
            if (mediaFileId > 0)
                tracker.UntrackAsync(entity, mediaFileId, path.ExtractPropertyInfo().Name);

            return Task.CompletedTask;
        }

        public static Task UntrackAsync<T>(this IMediaTracker tracker, T entity, Expression<Func<T, int?>> path) where T : BaseEntity, new()
        {
            Guard.NotNull(path, nameof(path));

            var getter = path.GetPropertyInvoker();
            var mediaFileId = getter.Invoke(entity);
            if (mediaFileId > 0)
                tracker.UntrackAsync(entity, mediaFileId.Value, path.ExtractPropertyInfo().Name);

            return Task.CompletedTask;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task TrackAsync(this IMediaTracker tracker, MediaTrack track)
        {
            if (track == null)
                return Task.CompletedTask;

            return tracker.TrackManyAsync(new[] { track });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task UpdateTracksAsync(this IMediaTracker tracker, BaseEntity entity, int? prevMediaFileId, int? currentMediaFileId, string propertyName)
        {
            return UpdateTracksAsync(tracker, entity.Id, entity.GetEntityName(), prevMediaFileId, currentMediaFileId, propertyName);
        }

        public static Task UpdateTracksAsync(this IMediaTracker tracker, int entityId, string entityName, int? prevMediaFileId, int? currentMediaFileId, string propertyName)
        {
            var tracks = new List<MediaTrack>(2);
            bool isModified = prevMediaFileId != currentMediaFileId;

            if (prevMediaFileId > 0 && isModified)
            {
                tracks.Add(new MediaTrack
                {
                    EntityId = entityId,
                    EntityName = entityName,
                    MediaFileId = prevMediaFileId.Value,
                    Property = propertyName,
                    Operation = MediaTrackOperation.Untrack
                });
            }

            if (currentMediaFileId > 0 && isModified)
            {
                tracks.Add(new MediaTrack
                {
                    EntityId = entityId,
                    EntityName = entityName,
                    MediaFileId = currentMediaFileId.Value,
                    Property = propertyName,
                    Operation = MediaTrackOperation.Track
                });
            }

            if (tracks.Count > 0)
            {
                return tracker.TrackManyAsync(tracks);
            }

            return Task.CompletedTask;
        }
    }
}