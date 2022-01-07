using System.Runtime.CompilerServices;
using Smartstore.Caching;
using Smartstore.Core.Data;
using Smartstore.Core.Identity;
using Smartstore.Data.Hooks;

namespace Smartstore.Core.Logging
{
    [Important]
    public partial class ActivityLogger : IActivityLogger
    {
        const string CacheKey = "activitylogtypes:dict";

        private readonly SmartDbContext _db;
        private readonly IWorkContext _workContext;
        private readonly IRequestCache _requestCache;

        public ActivityLogger(SmartDbContext db, IWorkContext workContext, IRequestCache requestCache)
        {
            _db = db;
            _workContext = workContext;
            _requestCache = requestCache;
        }

        public IEnumerable<ActivityLogType> GetAllActivityTypes()
        {
            return GetCachedActivityLogTypes().Values;
        }

        public ActivityLogType GetActivityTypeByKeyword(string keyword)
        {
            if (keyword.IsEmpty())
                return null;

            if (GetCachedActivityLogTypes().TryGetValue(keyword, out var logType))
            {
                return logType;
            }

            return null;
        }

        protected virtual IReadOnlyDictionary<string, ActivityLogType> GetCachedActivityLogTypes()
        {
            return _requestCache.Get(CacheKey, () =>
            {
                var all = _db.ActivityLogTypes
                    .AsNoTracking()
                    .ToList();

                return all.ToDictionarySafe(x => x.SystemKeyword);
            });
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual ActivityLog LogActivity(string activity, string comment, params object[] commentParams)
        {
            return LogActivity(activity, comment, _workContext.CurrentCustomer, commentParams);
        }

        public virtual ActivityLog LogActivity(string activity, string comment, Customer customer, params object[] commentParams)
        {
            if (customer == null)
                return null;

            var activityType = GetActivityTypeByKeyword(activity);
            if (activityType == null || !activityType.Enabled)
            {
                return null;
            }

            var entity = new ActivityLog
            {
                ActivityLogTypeId = activityType.Id,
                CustomerId = customer.Id,
                Comment = comment.EmptyNull().FormatCurrent(commentParams).Truncate(4000),
                CreatedOnUtc = DateTime.UtcNow
            };

            _db.ActivityLogs.Add(entity);

            return entity;
        }

        public virtual async Task ClearAllActivitiesAsync(CancellationToken cancelToken = default)
        {
            using var tx = await _db.Database.BeginTransactionAsync(cancelToken);

            await _db.DataProvider.TruncateTableAsync<ActivityLog>();
            await _db.DataProvider.SetTableIncrementAsync<ActivityLog>(1);

            await tx.CommitAsync(cancelToken);
        }
    }
}