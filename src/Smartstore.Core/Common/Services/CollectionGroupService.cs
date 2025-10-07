using Smartstore.Core.Catalog.Attributes;
using Smartstore.Core.Data;

namespace Smartstore.Core.Common.Services
{
    public partial class CollectionGroupService(SmartDbContext db) : ICollectionGroupService
    {
        private readonly SmartDbContext _db = db;

        public async Task<bool> ApplyCollectionGroupNameAsync<TEntity>(TEntity entity, string collectionGroupName)
            where TEntity : BaseEntity, IGroupedEntity
        {
            if (collectionGroupName.IsEmpty())
            {
                entity.CollectionGroupId = null;
                return true;
            }

            if (entity.Id != 0)
            {
                var entityName = entity.GetEntityName();
                var existingCollectionGroup = await _db.CollectionGroups
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.EntityName == entityName && x.Name == collectionGroupName);

                if (existingCollectionGroup == null)
                {
                    existingCollectionGroup = new CollectionGroup
                    {
                        EntityName = nameof(SpecificationAttributeOption),
                        Name = collectionGroupName,
                        Published = true,
                        DisplayOrder = await _db.CollectionGroups.MaxAsync(x => (int?)x.DisplayOrder) ?? 1
                    };

                    _db.CollectionGroups.Add(existingCollectionGroup);
                    await _db.SaveChangesAsync();

                    // TODO: Add mapping... EntityId = entity.Id
                }

                entity.CollectionGroupId = existingCollectionGroup.Id;
                return true;
            }

            return false;
        }
    }
}
