using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Content.Media
{
    [Important]
    internal sealed class MediaTrackerHook : AsyncDbSaveHook<BaseEntity>
    {
        // Track items for the current (SaveChanges) unit.
        private readonly HashSet<MediaTrack> _actionsUnit = new();

        // Track items already processed during the current request.
        private readonly HashSet<MediaTrack> _actionsAll = new();

        // Entities that are not saved yet but contain effective changes. We won't track if an error occurred during save.
        private readonly Dictionary<BaseEntity, HashSet<MediaTrack>> _actionsTemp = new();

        private readonly Lazy<IMediaTracker> _mediaTracker;

        public MediaTrackerHook(Lazy<IMediaTracker> mediaTracker)
        {
            _mediaTracker = mediaTracker;
        }

        internal static bool Silent { get; set; }

        protected override Task<HookResult> OnUpdatingAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => HookObject(entry, true);

        protected override Task<HookResult> OnDeletedAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => HookObject(entry, false);

        protected override Task<HookResult> OnInsertedAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => HookObject(entry, false);

        protected override Task<HookResult> OnUpdatedAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => HookObject(entry, false);

        private Task<HookResult> HookObject(IHookedEntity entry, bool beforeSave)
        {
            if (Silent)
                return Task.FromResult(HookResult.Ok);

            var type = entry.EntityType;

            if (!_mediaTracker.Value.TryGetTrackedPropertiesFor(type, out var properties))
            {
                return Task.FromResult(HookResult.Void);
            }

            var state = entry.InitialState;
            var actions = new HashSet<MediaTrack>();

            foreach (var prop in properties)
            {
                if (beforeSave)
                {
                    if (entry.Entry.TryGetModifiedProperty(prop.Name, out object prevValue))
                    {
                        // Untrack the previous file relation (if not null)
                        TryAddTrack(prop.Album, entry.Entity, prop.Name, prevValue, MediaTrackOperation.Untrack, actions);

                        // Track the new file relation (if not null)
                        TryAddTrack(prop.Album, entry.Entity, prop.Name, entry.Entry.CurrentValues[prop.Name], MediaTrackOperation.Track, actions);

                        _actionsTemp[entry.Entity] = actions;
                    }
                }
                else
                {
                    switch (state)
                    {
                        case EntityState.Added:
                        case EntityState.Deleted:
                            var value = type.GetProperty(prop.Name).GetValue(entry.Entity);
                            TryAddTrack(prop.Album, entry.Entity, prop.Name, value, state == EntityState.Added ? MediaTrackOperation.Track : MediaTrackOperation.Untrack);
                            break;
                        case EntityState.Modified:
                            if (_actionsTemp.TryGetValue(entry.Entity, out actions))
                            {
                                _actionsUnit.AddRange(actions);
                            }
                            break;
                    }
                }
            }

            return Task.FromResult(HookResult.Ok);
        }

        private void TryAddTrack(string album, BaseEntity entity, string prop, object value, MediaTrackOperation operation, HashSet<MediaTrack> actions = null)
        {
            if (value == null)
                return;

            if ((int)value > 0)
            {
                (actions ?? _actionsUnit).Add(new MediaTrack
                {
                    Album = album,
                    EntityId = entity.Id,
                    EntityName = entity.GetEntityName(),
                    Property = prop,
                    MediaFileId = (int)value,
                    Operation = operation
                });
            }
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            // Remove already processed items during this request.
            _actionsUnit.ExceptWith(_actionsAll);

            if (_actionsUnit.Count == 0)
            {
                return;
            }

            _actionsAll.UnionWith(_actionsUnit);

            // Commit all track items in one go
            var tracker = _mediaTracker.Value;
            using (tracker.BeginScope(false))
            {
                await tracker.TrackManyAsync(_actionsUnit);
            }

            _actionsUnit.Clear();
            _actionsTemp.Clear();
        }
    }
}