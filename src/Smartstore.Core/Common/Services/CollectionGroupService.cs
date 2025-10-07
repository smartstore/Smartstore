using Smartstore.Core.Data;

namespace Smartstore.Core.Common.Services
{
    public partial class CollectionGroupService(SmartDbContext db) : ICollectionGroupService
    {
        private readonly SmartDbContext _db = db;

        public async Task<bool> ApplyCollectionGroupNameAsync<TEntity>(TEntity entity, string collectionGroupName)
            where TEntity : BaseEntity, IGroupedEntity
        {
            if (entity.Id == 0)
            {
                return false;
            }

            if (collectionGroupName.IsEmpty())
            {
                if (entity.CollectionGroupMappingId != null)
                {
                    // Delete mapping.
                    await _db.LoadReferenceAsync(entity, x => x.CollectionGroupMapping, true);

                    _db.CollectionGroupMappings.Remove(entity.CollectionGroupMapping);
                    await _db.SaveChangesAsync();
                    return true;
                }

                return false;
            }

            var entityName = entity.GetEntityName();
            var existingGroup = await _db.CollectionGroups
                .AsNoTracking()
                .Include(x => x.CollectionGroupMappings)
                .FirstOrDefaultAsync(x => x.Name == collectionGroupName && x.EntityName == entityName);

            if (existingGroup == null)
            {
                // Add collection group.
                var displayOrder = await _db.CollectionGroups
                    .Where(x => x.EntityName == entityName)
                    .MaxAsync(x => (int?)x.DisplayOrder) ?? 0;

                existingGroup = new CollectionGroup
                {
                    EntityName = entityName,
                    Name = collectionGroupName,
                    DisplayOrder = ++displayOrder
                };

                _db.CollectionGroups.Add(existingGroup);
                await _db.SaveChangesAsync();
            }

            var existingMapping = existingGroup.CollectionGroupMappings.FirstOrDefault(x => x.EntityId == entity.Id);
            if (existingMapping == null)
            {
                // Add mapping.
                existingMapping = new CollectionGroupMapping
                {
                    CollectionGroupId = existingGroup.Id,
                    EntityId = entity.Id
                };

                _db.CollectionGroupMappings.Add(existingMapping);
                await _db.SaveChangesAsync();
                return true;
            }

            if (entity.CollectionGroupMappingId != existingMapping.Id)
            {
                entity.CollectionGroupMappingId = existingMapping.Id;
                return true;
            }

            return false;
        }
    }
}
