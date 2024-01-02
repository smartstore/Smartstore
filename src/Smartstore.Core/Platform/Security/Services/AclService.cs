using System.Runtime.CompilerServices;
using System.Text;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Security
{
    [Important]
    public partial class AclService : AsyncDbSaveHook<AclRecord>, IAclService
    {
        /// <summary>
        /// 0 = segment (EntityName.IdRange)
        /// </summary>
        private readonly static CompositeFormat ACL_SEGMENT_KEY = CompositeFormat.Parse("acl:range-{0}");
        internal const string ACL_SEGMENT_PATTERN = "acl:range-*";

        private readonly SmartDbContext _db;
        private readonly Work<IWorkContext> _workContext;
        private readonly ICacheManager _cache;
        private bool? _hasActiveAcl;

        public AclService(
            SmartDbContext db,
            Work<IWorkContext> workContext,
            ICacheManager cache)
        {
            _db = db;
            _workContext = workContext;
            _cache = cache;
        }

        #region Hook

        public override Task<HookResult> OnAfterSaveAsync(IHookedEntity entry, CancellationToken cancelToken)
            => Task.FromResult(HookResult.Ok);

        public override async Task OnAfterSaveCompletedAsync(IEnumerable<IHookedEntity> entries, CancellationToken cancelToken)
        {
            var distinctEntries = entries
                .Select(x => x.Entity)
                .OfType<AclRecord>()
                .Select(x => (x.EntityName, x.EntityId))
                .Distinct()
                .ToArray();

            foreach (var entry in distinctEntries)
            {
                await ClearCacheSegmentAsync(entry.EntityName, entry.EntityId);
            }
        }

        #endregion

        public virtual bool HasActiveAcl()
        {
            if (!_hasActiveAcl.HasValue)
            {
                _hasActiveAcl = _db.AclRecords.Any(x => !x.IsIdle);
            }

            return _hasActiveAcl.Value;
        }

        public virtual async Task<bool> HasActiveAclAsync()
        {
            if (!_hasActiveAcl.HasValue)
            {
                _hasActiveAcl = await _db.AclRecords.AnyAsync(x => !x.IsIdle);
            }

            return _hasActiveAcl.Value;
        }

        public virtual async Task ApplyAclMappingsAsync<T>(T entity, int[] selectedCustomerRoleIds)
            where T : BaseEntity, IAclRestricted
        {
            Guard.NotNull(entity);

            selectedCustomerRoleIds ??= Array.Empty<int>();

            var existingAclRecords = await _db.AclRecords
                .ApplyEntityFilter(entity)
                .ToListAsync();

            var allCustomerRoles = await _db.CustomerRoles
                .AsNoTracking()
                .ToListAsync();

            entity.SubjectToAcl = (selectedCustomerRoleIds.Length != 1 || selectedCustomerRoleIds[0] != 0) && selectedCustomerRoleIds.Any();

            foreach (var customerRole in allCustomerRoles)
            {
                if (selectedCustomerRoleIds != null && selectedCustomerRoleIds.Contains(customerRole.Id))
                {
                    if (!existingAclRecords.Any(x => x.CustomerRoleId == customerRole.Id))
                    {
                        _db.AclRecords.Add(new AclRecord
                        {
                            EntityId = entity.Id,
                            EntityName = entity.GetEntityName(),
                            CustomerRoleId = customerRole.Id
                        });
                    }
                }
                else
                {
                    var aclRecordToDelete = existingAclRecords.FirstOrDefault(x => x.CustomerRoleId == customerRole.Id);
                    if (aclRecordToDelete != null)
                    {
                        _db.AclRecords.Remove(aclRecordToDelete);
                    }
                }
            }
        }

        public virtual async Task<int[]> GetAuthorizedCustomerRoleIdsAsync(string entityName, int entityId)
        {
            Guard.NotEmpty(entityName);

            if (entityId <= 0)
            {
                return Array.Empty<int>();
            }

            var cacheSegment = await GetCacheSegmentAsync(entityName, entityId);

            if (!cacheSegment.TryGetValue(entityId, out var roleIds))
            {
                return Array.Empty<int>();
            }

            return roleIds;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual async Task<bool> AuthorizeAsync(string entityName, int entityId)
        {
            return await AuthorizeAsync(entityName, entityId, _workContext.Value.CurrentCustomer?.CustomerRoleMappings?.Select(x => x.CustomerRole));
        }

        public virtual async Task<bool> AuthorizeAsync(string entityName, int entityId, IEnumerable<CustomerRole> roles)
        {
            Guard.NotEmpty(entityName);

            if (entityId <= 0)
            {
                return false;
            }

            if (!await HasActiveAclAsync())
            {
                return true;
            }

            if (roles == null)
            {
                return false;
            }

            foreach (var role in roles.Where(x => x.Active))
            {
                var authorizedRoleIds = await GetAuthorizedCustomerRoleIdsAsync(entityName, entityId);

                foreach (var roleId in authorizedRoleIds)
                {
                    if (role.Id == roleId)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #region Cache segmenting

        protected virtual Task<Dictionary<int, int[]>> GetCacheSegmentAsync(string entityName, int entityId)
        {
            Guard.NotEmpty(entityName);

            var segmentKey = GetSegmentKeyPart(entityName, entityId, out var minEntityId, out var maxEntityId);
            var cacheKey = BuildCacheSegmentKey(segmentKey);

            var result = _cache.GetAsync(cacheKey, async () =>
            {
                var query =
                    from x in _db.AclRecords.AsNoTracking()
                    where
                        x.EntityId >= minEntityId &&
                        x.EntityId <= maxEntityId &&
                        x.EntityName == entityName
                    select x;

                var mappings = (await query.ToListAsync()).ToLookup(x => x.EntityId, x => x.CustomerRoleId);
                var dict = new Dictionary<int, int[]>(mappings.Count);

                foreach (var sm in mappings)
                {
                    dict[sm.Key] = sm.ToArray();
                }

                return dict;
            });

            return result;
        }

        protected virtual Task ClearCacheSegmentAsync(string entityName, int entityId)
        {
            try
            {
                var segmentKey = GetSegmentKeyPart(entityName, entityId);
                return _cache.RemoveAsync(BuildCacheSegmentKey(segmentKey));
            }
            catch
            {
                return Task.CompletedTask;
            }
        }

        private static string BuildCacheSegmentKey(string segment)
        {
            return ACL_SEGMENT_KEY.FormatInvariant(segment);
        }

        private static string GetSegmentKeyPart(string entityName, int entityId)
        {
            return GetSegmentKeyPart(entityName, entityId, out _, out _);
        }

        private static string GetSegmentKeyPart(string entityName, int entityId, out int minId, out int maxId)
        {
            (minId, maxId) = entityId.GetRange(1000);
            return (entityName + "." + minId.ToString()).ToLowerInvariant();
        }

        #endregion
    }
}
