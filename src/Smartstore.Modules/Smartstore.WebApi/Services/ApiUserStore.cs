using Autofac;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api
{
    public interface IApiUserStore
    {
        int SaveApiUsers(Dictionary<string, WebApiUser> users);
        Task<int> SaveApiUsersAsync(Dictionary<string, WebApiUser> users);
    }

    public class ApiUserStore : IApiUserStore
    {
        //private readonly IComponentContext _scope;
        //private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IDbContextFactory<SmartDbContext> _dbContextFactory;

        public ApiUserStore(IDbContextFactory<SmartDbContext> dbContextFactory)
        {
            _dbContextFactory = dbContextFactory;
        }


        //public ApiUserStore(
        //    IHttpContextAccessor httpContextAccessor,
        //    IDbContextFactory<SmartDbContext> dbContextFactory)
        //    : this(null, httpContextAccessor, dbContextFactory)
        //{
        //}

        //internal ApiUserStore(
        //    IComponentContext scope,
        //    IHttpContextAccessor httpContextAccessor,
        //    IDbContextFactory<SmartDbContext> dbContextFactory)
        //{
        //    _scope = scope;
        //    _httpContextAccessor = httpContextAccessor;
        //    _dbContextFactory = dbContextFactory;
        //}

        public Task<int> SaveApiUsersAsync(Dictionary<string, WebApiUser> users)
            => SaveApiUsersInternal(users, true);

        public int SaveApiUsers(Dictionary<string, WebApiUser> users)
            => SaveApiUsersInternal(users, false).Await();

        internal async Task<int> SaveApiUsersInternal(Dictionary<string, WebApiUser> users, bool async)
        {
            // INFO: (mg) (core) Follow this "*Internal(..., bool async)" pattern where possible.
            var num = 0;

            if (users != null)
            {
                var usersToStore = users.Values.Where(x => x.IsStoringRequired && x.IsValid).ToList();
                if (usersToStore.Count > 0)
                {
                    using var db = _dbContextFactory.CreateDbContext();

                    foreach (var chunk in usersToStore.Chunk(100))
                    {
                        var ids = chunk.ToDistinctArray(x => x.GenericAttributeId);
                        var attributes = db.GenericAttributes
                            .Where(x => ids.Contains(x.Id))
                            .ToDictionary(x => x.Id);

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
                        num += async ? await db.SaveChangesAsync() : db.SaveChanges();
                    }
                }
            }

            return num;
        }

        //private IDisposable GetOrCreateDbContext(out SmartDbContext db)
        //{
        //    db = _scope?.ResolveOptional<SmartDbContext>() ??
        //         _httpContextAccessor.HttpContext?.RequestServices?.GetService<SmartDbContext>();

        //    if (db != null)
        //    {
        //        // Don't dispose request scoped main db instance.
        //        return ActionDisposable.Empty;
        //    }

        //    db = _dbContextFactory.CreateDbContext();

        //    return db;
        //}
    }
}
