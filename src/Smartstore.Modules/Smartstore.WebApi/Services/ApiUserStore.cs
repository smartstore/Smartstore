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
                _timer ??= new(_ => OnTick());
                _timer.Start(storingInterval);
            }
        }

        private async Task OnTick()
        {
            if (_memCache.TryGetValue(WebApiService.UsersKey, out object cachedUsers))
            {
                await SaveApiUsersInternal(cachedUsers as Dictionary<string, WebApiUser>, true);
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
            //using var db = EngineContext.Current.Application.Services.Resolve<IDbContextFactory<SmartDbContext>>().CreateDbContext();

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

                // TODO: (mg) (core) System.ObjectDisposedException: "Instances cannot be resolved and nested lifetimes cannot be created from this LifetimeScope... already been disposed."
                // RE: Hmmm?... what about "EngineContext.Current.Application.Services.Resolve<IDbContextFactory<SmartDbContext>>().CreateDbContext()"?
                //      Does that work? If yes, I'm gonna have a look at how the factory is resolved in ctor. This shouldn't happen (because the factory is singleton).
                // RE RE: no, same error but ApiUserStore2 below works.
                num += async ? await db.SaveChangesAsync() : db.SaveChanges();
            }

            _lastSavingDate = DateTime.UtcNow;
            $"Saved {num} API user.".Dump();

            return num;
        }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();
            }
        }

        protected override async ValueTask OnDisposeAsync(bool disposing)
        {
            if (disposing)
            {
                _timer?.Dispose();

                // Persist remaining data on dispose.
                await OnTick();
            }
        }
    }


    //public interface IApiUserStore2
    //{
    //    int SaveApiUsers(Dictionary<string, WebApiUser> users, bool useDbContextFactory = true);
    //    Task<int> SaveApiUsersAsync(Dictionary<string, WebApiUser> users, bool useDbContextFactory = true);
    //    void Activate(TimeSpan storingInterval);
    //}

    //public class ApiUserStore2 : Disposable, IApiUserStore2
    //{
    //    const string UpdateSql = "Update GenericAttribute Set Value = {0} Where Id = {1}";

    //    private readonly IComponentContext _scope;
    //    private readonly IHttpContextAccessor _httpContextAccessor;
    //    private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;
    //    private readonly IMemoryCache _memCache;
    //    private Timer _timer;
    //    private DateTime _lastSavingDate = DateTime.UtcNow;

    //    public ApiUserStore2(
    //        IComponentContext scope,
    //        IHttpContextAccessor httpContextAccessor,
    //        IDbContextFactory<SmartDbContext> dbContextFactory,
    //        IMemoryCache memCache)
    //    {
    //        _scope = scope;
    //        _httpContextAccessor = httpContextAccessor;
    //        _dbContextFactory = dbContextFactory;
    //        _memCache = memCache;
    //    }

    //    public Task<int> SaveApiUsersAsync(Dictionary<string, WebApiUser> users, bool useDbContextFactory = true)
    //        => SaveApiUsersInternal(users, useDbContextFactory, true);

    //    public int SaveApiUsers(Dictionary<string, WebApiUser> users, bool useDbContextFactory = true)
    //        => SaveApiUsersInternal(users, useDbContextFactory, false).Await();

    //    public void Activate(TimeSpan storingInterval)
    //    {
    //        _lastSavingDate = DateTime.UtcNow;

    //        _timer ??= new(async state =>
    //        {
    //            if (_memCache.TryGetValue(WebApiService.UsersKey, out object cachedUsers))
    //            {
    //                await SaveApiUsersInternal(cachedUsers as Dictionary<string, WebApiUser>, true, true);
    //            }
    //        }, 
    //        null, storingInterval, storingInterval);
    //    }

    //    protected override void OnDispose(bool disposing)
    //    {
    //        if (disposing)
    //        {
    //            _timer?.Dispose();
    //        }
    //    }

    //    private async Task<int> SaveApiUsersInternal(Dictionary<string, WebApiUser> users, bool useDbContextFactory, bool async)
    //    {
    //        var usersToStore = users?.Values?.Where(x => x.LastRequest.HasValue && x.LastRequest > _lastSavingDate && x.IsValid)?.ToList();
    //        if (usersToStore.IsNullOrEmpty())
    //        {
    //            return 0;
    //        }

    //        var num = 0;
    //        using (GetOrCreateDbContext(useDbContextFactory, out var db))
    //        {
    //            foreach (var user in usersToStore)
    //            {
    //                num += async
    //                    ? await db.Database.ExecuteSqlRawAsync(UpdateSql, user.ToString(), user.GenericAttributeId)
    //                    : db.Database.ExecuteSqlRaw(UpdateSql, user.ToString(), user.GenericAttributeId);
    //            }

    //            // Does not work if useDbContextFactory = false. Freezing... corrupt process.
    //            //using var transaction = db.Database.BeginTransaction();

    //            //foreach (var user in usersToStore)
    //            //{
    //            //    db.Database.ExecuteSqlRaw(UpdateSql, user.ToString(), user.GenericAttributeId);
    //            //}

    //            //num = db.SaveChanges();
    //            //transaction.Commit();
    //        }

    //        _lastSavingDate = DateTime.UtcNow;

    //        //$"Saved {num} API user.".Dump();
    //        return num;
    //    }

    //    private IDisposable GetOrCreateDbContext(bool useDbContextFactory, out DbContext db)
    //    {
    //        if (useDbContextFactory)
    //        {
    //            db = _scope?.ResolveOptional<SmartDbContext>() ??
    //                 _httpContextAccessor.HttpContext?.RequestServices?.GetService<SmartDbContext>();

    //            if (db != null)
    //            {
    //                // Don't dispose request scoped main db instance.
    //                return ActionDisposable.Empty;
    //            }

    //            db = _dbContextFactory.CreateDbContext();
    //        }
    //        else
    //        {
    //            var settings = DataSettings.Instance;
    //            db = settings.DbFactory.CreateDbContext<DbContext>(settings.ConnectionString);
    //        }

    //        return db;
    //    }
    //}
}
