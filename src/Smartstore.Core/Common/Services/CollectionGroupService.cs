using System.Security;
using Smartstore.Core.Data;
using Smartstore.Core.Security;

namespace Smartstore.Core.Common.Services
{
    public partial class CollectionGroupService : ICollectionGroupService
    {
        private readonly SmartDbContext _db;
        private readonly IPermissionService _permissionService;

        public CollectionGroupService(SmartDbContext db, IPermissionService permissionService)
        {
            _db = db;
            _permissionService = permissionService;
        }

        public async Task<bool> ApplyCollectionGroupNameAsync<TEntity>(TEntity entity, string collectionGroupName)
            where TEntity : BaseEntity, IGroupedEntity
        {
            if (entity.Id == 0)
            {
                return false;
            }

            if (entity.CollectionGroupMappingId != null && entity.CollectionGroupMapping == null)
            {
                await _db.LoadReferenceAsync(entity, x => x.CollectionGroupMapping, false, q => q.Include(x => x.CollectionGroup));
            }

            var mapping = entity.CollectionGroupMapping;

            if (collectionGroupName.IsEmpty())
            {
                if (mapping != null)
                {
                    _db.CollectionGroupMappings.Remove(mapping);
                }

                return mapping != null;
            }

            // Perf: Exit if the new name is the old one.
            if (mapping?.CollectionGroup?.Name == collectionGroupName)
            {
                return false;
            }

            var updated = true;
            var entityName = entity.GetEntityName();
            var existingGroup = await _db.CollectionGroups
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Name == collectionGroupName && x.EntityName == entityName);

            if (existingGroup == null)
            {
                if (!await _permissionService.AuthorizeAsync(Permissions.Configuration.CollectionGroup.Create))
                {
                    throw new SecurityException(await _permissionService.GetUnauthorizedMessageAsync(Permissions.Configuration.CollectionGroup.Create));
                }

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

            if (mapping != null)
            {
                // Update mapping. A different collection group may be selected.
                updated = mapping.CollectionGroupId != existingGroup.Id;

                mapping.CollectionGroupId = existingGroup.Id;
            }
            else
            {
                // Add mapping. Entity is not assigned to any collection group.
                entity.CollectionGroupMapping = new CollectionGroupMapping
                {
                    CollectionGroupId = existingGroup.Id,
                    EntityId = entity.Id
                };
            }

            return updated;
        }
    }
}
