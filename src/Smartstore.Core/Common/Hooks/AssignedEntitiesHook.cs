using System.Collections.Frozen;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Content.Media;
using Smartstore.Core.Data;
using Smartstore.Core.Messaging;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    /// <summary>
    /// Deletes assigned entities of entities that were deleted by referential integrity.
    /// Without explicit deletion these assigned entities would remain in the database forever.
    /// Typically used for satellite entities like LocalizedProperty or MediaTrack that have no foreign key relationships.
    /// </summary>
    [Important]
    internal class AssignedEntitiesHook(SmartDbContext db) : AsyncDbSaveHook<BaseEntity>
    {
        const int BatchSize = 128;

        private static readonly FrozenSet<Type> _candidateTypes = new Type[]
        {
            typeof(ProductAttribute),
            typeof(ProductVariantAttribute),
            typeof(ProductAttributeOptionsSet),
            typeof(SpecificationAttribute),
            typeof(QueuedEmail)
        }.ToFrozenSet();

        private readonly SmartDbContext _db = db;
        private readonly List<AssignedItem> _assignedItems = [];

        protected override Task<HookResult> OnDeletingAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(_candidateTypes.Contains(entry.EntityType) ? HookResult.Ok : HookResult.Void);

        protected override Task<HookResult> OnDeletedAsync(BaseEntity entity, IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(_candidateTypes.Contains(entry.EntityType) ? HookResult.Ok : HookResult.Void);

        public override async Task OnBeforeSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var groups = entries
                .Where(x => x.InitialState == EntityState.Deleted && _candidateTypes.Contains(x.EntityType))
                .GroupBy(x => x.EntityType);

            foreach (var group in groups)
            {
                var deletedEntityIds = group.ToDistinctArray(x => x.Entity.Id);

                if (group.Key == typeof(ProductAttribute))
                {
                    var optionIdsQuery =
                        from a in _db.ProductAttributes.AsNoTracking()
                        from os in a.ProductAttributeOptionsSets
                        from ao in os.ProductAttributeOptions
                        where deletedEntityIds.Contains(a.Id)
                        select ao.Id;

                    _assignedItems.Add(new()
                    {
                        EntityName = nameof(ProductAttributeOption),
                        EntityIds = await optionIdsQuery.ToArrayAsync(cancelToken)
                    });
                }
                else if (group.Key == typeof(ProductVariantAttribute))
                {
                    _assignedItems.Add(new()
                    {
                        EntityName = nameof(ProductVariantAttributeValue),
                        EntityIds = await _db.ProductVariantAttributeValues
                            .Where(x => deletedEntityIds.Contains(x.ProductVariantAttributeId))
                            .Select(x => x.Id)
                            .ToArrayAsync(cancelToken)
                    });
                }
                else if (group.Key == typeof(ProductAttributeOptionsSet))
                {
                    _assignedItems.Add(new()
                    {
                        EntityName = nameof(ProductAttributeOption),
                        EntityIds = await _db.ProductAttributeOptions
                            .Where(x => deletedEntityIds.Contains(x.ProductAttributeOptionsSetId))
                            .Select(x => x.Id)
                            .ToArrayAsync(cancelToken)
                    });
                }
                else if (group.Key == typeof(SpecificationAttribute))
                {
                    _assignedItems.Add(new()
                    {
                        EntityName = nameof(SpecificationAttributeOption),
                        EntityIds = await _db.SpecificationAttributeOptions
                            .Where(x => deletedEntityIds.Contains(x.SpecificationAttributeId))
                            .Select(x => x.Id)
                            .ToArrayAsync(cancelToken)
                    });
                }
                else if (group.Key == typeof(QueuedEmail))
                {
                    _assignedItems.Add(new()
                    {
                        AssignedEntities = AssignedEntity.MediaStorage,
                        EntityName = nameof(MediaStorage),
                        EntityIds = await _db.QueuedEmailAttachments
                            .Where(x => deletedEntityIds.Contains(x.QueuedEmailId) && x.MediaStorageId > 0)
                            .Select(x => x.MediaStorageId ?? 0)
                            .ToArrayAsync(cancelToken)
                    });
                }
            }                
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_assignedItems.Count == 0)
            {
                return;
            }

            foreach (var item in _assignedItems)
            {
                foreach (var ids in item.EntityIds.Chunk(BatchSize))
                {
                    if (item.AssignedEntities.HasFlag(AssignedEntity.LocalizedProperty))
                    {
                        await _db.LocalizedProperties
                            .Where(x => ids.Contains(x.EntityId) && x.LocaleKeyGroup == item.EntityName)
                            .ExecuteDeleteAsync(cancelToken);
                    }

                    if (item.AssignedEntities.HasFlag(AssignedEntity.MediaTrack))
                    {
                        await _db.MediaTracks
                            .Where(x => ids.Contains(x.EntityId) && x.EntityName == item.EntityName)
                            .ExecuteDeleteAsync(cancelToken);
                    }

                    if (item.AssignedEntities.HasFlag(AssignedEntity.MediaStorage))
                    {
                        await _db.MediaStorage
                            .Where(x => ids.Contains(x.Id))
                            .ExecuteDeleteAsync(cancelToken);
                    }
                }
            }

            _assignedItems.Clear();
        }


        [Flags]
        enum AssignedEntity
        {
            LocalizedProperty = 1 << 0,
            MediaTrack = 1 << 1,
            MediaStorage = 1 << 2,
        }

        record AssignedItem
        {
            /// <summary>
            /// The assigned entities to delete.
            /// </summary>
            public AssignedEntity AssignedEntities { get; set; } = AssignedEntity.LocalizedProperty | AssignedEntity.MediaTrack;

            /// <summary>
            /// The name of the assigned entities.
            /// </summary>
            public string EntityName { get; set; }

            /// <summary>
            /// IDs of entities to delete.
            /// </summary>
            public int[] EntityIds { get; set; }
        }
    }
}
