using System.Linq.Dynamic.Core;
using Smartstore.Core.Data;
using Smartstore.Data;

namespace Smartstore.Core.Localization
{
    public class LocalizedEntityLoader : ILocalizedEntityLoader
    {
        private readonly SmartDbContext _db;

        public LocalizedEntityLoader(SmartDbContext db)
        {
            _db = db;
        }

        public List<dynamic> Load(LocalizedEntityDescriptor descriptor)
        {
            return LoadInternal(descriptor, false).Await();
        }

        public Task<List<dynamic>> LoadAsync(LocalizedEntityDescriptor descriptor)
        {
            return LoadInternal(descriptor, true);
        }

        public DynamicFastPager LoadPaged(LocalizedEntityDescriptor descriptor, int pageSize = 1000)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            var query = CreateQuery(descriptor);
            var pager = new DynamicFastPager(query, pageSize);

            return pager;
        }

        private async Task<List<dynamic>> LoadInternal(LocalizedEntityDescriptor descriptor, bool async)
        {
            Guard.NotNull(descriptor, nameof(descriptor));

            var query = CreateQuery(descriptor);

            var list = async ? await query.ToDynamicListAsync() : query.ToDynamicList();

            return list;
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
                .Select($"new {{ Id,  {string.Join(", ", descriptor.PropertyNames)} }}");

            return query;
        }
    }
}
