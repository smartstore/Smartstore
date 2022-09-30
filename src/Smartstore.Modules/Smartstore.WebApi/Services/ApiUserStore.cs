using Autofac;
using Microsoft.Extensions.Caching.Memory;
using Smartstore.Threading;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api
{
    public interface IApiUserStore
    {
        void Activate(TimeSpan storingInterval);
        int SaveApiUsers(Dictionary<string, WebApiUser> users);
        Task<int> SaveApiUsersAsync(Dictionary<string, WebApiUser> users);
    }

    public class ApiUserStore : Disposable, IApiUserStore
    {
        private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;
        private readonly IMemoryCache _memCache;

        private PortableTimer _timer;
        private DateTime _lastSavingDate = DateTime.UtcNow;

        public ApiUserStore(IDbContextFactory<SmartDbContext> dbContextFactory, IMemoryCache memCache)
        {
            _dbContextFactory = dbContextFactory;
            _memCache = memCache;
        }

        public void Activate(TimeSpan storingInterval)
        {
            _lastSavingDate = DateTime.UtcNow;

            if (_timer == null)
            {
                _timer ??= new(_ => OnTick(true));
                _timer.Start(storingInterval);
            }
        }

        private async Task OnTick(bool async)
        {
            if (_memCache.TryGetValue(WebApiService.UsersKey, out object cachedUsers))
            {
                await SaveApiUsersInternal(cachedUsers as Dictionary<string, WebApiUser>, async);
            }
        }

        public Task<int> SaveApiUsersAsync(Dictionary<string, WebApiUser> users)
            => SaveApiUsersInternal(users, true);

        public int SaveApiUsers(Dictionary<string, WebApiUser> users)
            => SaveApiUsersInternal(users, false).Await();

        internal async Task<int> SaveApiUsersInternal(Dictionary<string, WebApiUser> users, bool async)
        {
            var usersToStore = users?.Values?.Where(x => x.LastRequest.HasValue && x.LastRequest > _lastSavingDate && x.IsValid)?.ToList();
            if (usersToStore.IsNullOrEmpty())
            {
                return 0;
            }

            var num = 0;
            using var db = _dbContextFactory.CreateDbContext();

            foreach (var chunk in usersToStore.Chunk(100))
            {
                var ids = chunk.ToDistinctArray(x => x.GenericAttributeId);
                var query = db.GenericAttributes.Where(x => ids.Contains(x.Id));
                var attributes = async ? await query.ToDictionaryAsync(x => x.Id) : query.ToDictionary(x => x.Id);

                foreach (var user in chunk)
                {
                    if (attributes.TryGetValue(user.GenericAttributeId, out var attribute))
                    {
                        attribute.Value = user.ToString();
                    }
                }

                num += async ? await db.SaveChangesAsync() : db.SaveChanges();
            }

            _lastSavingDate = DateTime.UtcNow;
            //$"Saved {num} API users.".Dump();

            return num;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();

                // Persist remaining data on dispose.
                OnTick(false).Await();
            }
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();

                // INFO: this disposer is called when the root container
                // is being disposed. So: no scopes from here on. That's why
                // OnTick() failed.

                // Persist remaining data on dispose.
                await OnTick(true);
            }
        }
    }
}
