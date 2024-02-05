using System.Collections.Frozen;
using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Common.Hooks
{
    /// <summary>
    /// Deletes assigned entities of entities that were deleted by referential integrity.
    /// Without explicit deletion these assigned entities (like LocalizedProperty or MediaTrack) would remain in the database forever.
    /// </summary>
    [Important]
    internal class AssignedEntitiesHook : AsyncDbSaveHook<BaseEntity>
    {
        private static readonly FrozenSet<Type> _candidateTypes = new Type[]
        {
            typeof(ProductAttribute),
            typeof(ProductVariantAttribute),
            typeof(ProductAttributeOptionsSet),
            typeof(SpecificationAttribute)
        }.ToFrozenSet();

        private readonly SmartDbContext _db;
        private readonly List<AssignedItem> _assignedItems = [];

        public AssignedEntitiesHook(SmartDbContext db)
        {
            _db = db;
        }

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
                var deletedEntityIds = group.Select(x => x.Entity.Id).ToArray();

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
            }                
        }

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            if (_assignedItems.Count > 0)
            {
                foreach (var item in _assignedItems)
                {
                    foreach (var ids in item.EntityIds.Chunk(100))
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
                    }
                }

                _assignedItems.Clear();
            }
        }


        [Flags]
        enum AssignedEntity
        {
            LocalizedProperty = 1 << 0,
            MediaTrack = 1 << 1
        }

        class AssignedItem
        {
            public AssignedEntity AssignedEntities { get; set; } = AssignedEntity.LocalizedProperty | AssignedEntity.MediaTrack;
            public string EntityName { get; set; }
            public int[] EntityIds { get; set; }
        }
    }
}
