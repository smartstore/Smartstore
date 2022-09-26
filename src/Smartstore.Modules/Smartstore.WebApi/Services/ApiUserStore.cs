using Autofac;
using Microsoft.Extensions.DependencyInjection;
using Smartstore.Utilities;
using Smartstore.Web.Api.Models;

namespace Smartstore.Web.Api
{
    public interface IApiUserStore
    {
        int SaveApiUsers(Dictionary<string, WebApiUser> users);
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

        public int SaveApiUsers(Dictionary<string, WebApiUser> users)
        {
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
                        num += db.SaveChanges();
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
