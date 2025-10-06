using Smartstore.Core.Data;

namespace Smartstore.Core.Common.Services
{
    public partial class CollectionGroupService
    {
        private readonly SmartDbContext _db;

        public CollectionGroupService(SmartDbContext db)
        {
            _db = db;
        }

        public async Task UpdateCollectionGroupsAsync(int entityId, string entityName, IEnumerable<string> groupNames)
        {
            Guard.NotZero(entityId);
            Guard.NotEmpty(entityName);

            var existingGroups = await _db.CollectionGroups
                .ApplyEntityFilter(entityName, [entityId], true)
                .ToListAsync();
            var displayOrder = existingGroups.Count > 0 ? existingGroups.Max(x => x.DisplayOrder) : 0;

            // Check whether to remove all groups.
            if (groupNames.IsNullOrEmpty() && existingGroups.Count > 0)
            {
                _db.CollectionGroups.RemoveRange(existingGroups);
                await _db.SaveChangesAsync();
                return;
            }

            // Groups to remove.
            var toRemove = new List<CollectionGroup>();
            var newNames = new HashSet<string>(groupNames
                .Select(x => x.TrimSafe())
                .Where(x => x.HasValue()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var existingGroup in existingGroups)
            {
                if (!newNames.Any(existingGroup.Name.EqualsNoCase))
                {
                    toRemove.Add(existingGroup);
                }
            }

            if (toRemove.Count > 0)
            {
                _db.CollectionGroups.RemoveRange(toRemove);
            }

            // Groups to add.
            foreach (var name in newNames)
            {
                if (!toRemove.Any(x => x.Name.EqualsNoCase(name)) && !existingGroups.Any(x => x.Name.Equals(name)))
                {
                    _db.CollectionGroups.Add(new()
                    {
                        Name = name,
                        DisplayOrder = ++displayOrder
                    });
                }
            }

            await _db.SaveChangesAsync();
        }
    }
}
