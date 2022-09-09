using System.Linq.Dynamic.Core;
using Autofac;
using Smartstore.Core.Data;
using Smartstore.Data;

namespace Smartstore.Core.Localization
{
    public class LocalizedEntityLoader : ILocalizedEntityLoader
    {
        private readonly ILifetimeScope _scope;
        private readonly SmartDbContext _db;

        public LocalizedEntityLoader(ILifetimeScope scope, SmartDbContext db)
        {
            _scope = scope;
            _db = db;
        }

        public IList<dynamic> Load(LocalizedEntityDescriptor descriptor)
        {
            return LoadInternal(descriptor, false).Await();
        }

        public Task<IList<dynamic>> LoadAsync(LocalizedEntityDescriptor descriptor)
        {
            return LoadInternal(descriptor, true);
        }

        public DynamicFastPager LoadPaged(LocalizedEntityDescriptor descriptor, int pageSize = 1000)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            var query = CreateQuery(descriptor);
            var pager = new DynamicFastPager(query, pageSize)
            {
                ResultExtender = list => ExtendResult(list, descriptor)
            };

            return pager;
        }

        public async Task<IList<dynamic>> LoadByDelegateAsync(LoadLocalizedEntityDelegate @delegate)
        {
            Guard.NotNull(@delegate, nameof(@delegate));

            return await @delegate(_scope, _db);
        }

        private async Task<IList<dynamic>> LoadInternal(LocalizedEntityDescriptor descriptor, bool async)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            var query = CreateQuery(descriptor);
            
            var list = async ? await query.ToDynamicListAsync() : query.ToDynamicList();

            return ExtendResult(list, descriptor);
        }

        protected virtual IQueryable CreateQuery(LocalizedEntityDescriptor descriptor)
        {
            // --> _db.Set<EntityType>()
            var methodInfo = _db.GetType()
                .GetMethod("Set", Array.Empty<Type>())
                .MakeGenericMethod(descriptor.EntityType);

            // Call Set<EntityType>() and cast to IQueryable, so that we can use DynamicLinq.
            var query = (IQueryable)methodInfo.Invoke(_db, null);

            if (descriptor.FilterPredicate.HasValue())
            {
                query = query.Where(descriptor.FilterPredicate);
            }

            query = query
                // --> new { Id, Name, ShortDescription, FullDescription }
                .Select($"new {{ Id, {string.Join(", ", descriptor.PropertyNames)} }}");

            return query;
        }

        /// <summary>
        /// Adds "KeyGroup" property to all dynamic objects, reading the value from <paramref name="descriptor"/>.KeyGroup.
        /// </summary>
        private static IList<dynamic> ExtendResult(IList<dynamic> result, LocalizedEntityDescriptor descriptor)
        {
            foreach (var obj in result.OfType<DynamicClass>())
            {
                obj["KeyGroup"] = descriptor.KeyGroup;
            }

            return result;
        }
    }
}
