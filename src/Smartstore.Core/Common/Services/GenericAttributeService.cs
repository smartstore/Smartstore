using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;
using Smartstore.Core.Stores;

namespace Smartstore.Core.Common.Services
{
    public partial class GenericAttributeService : IGenericAttributeService
    {
        // TODO: (core) Implement GenericAttributeService.

        private readonly SmartDbContext _db;
        private readonly IStoreContext _storeContext;

        public GenericAttributeService(SmartDbContext db, IStoreContext storeContext)
        {
            _db = db;
            _storeContext = storeContext;
        }

        public virtual async Task<GenericAttributeCollection> GetAttributesForEntityAsync(int entityId, string entityName, int storeId = 0)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            // TODO: (core) Implement "GetAttributesForEntity" request caching
            // TODO: (core) Implement "Reload()" for GenericAttributeCollection

            if (storeId <= 0)
            {
                storeId = _storeContext.CurrentStore.Id;
            }

            if (entityId <= 0)
            {
                return new GenericAttributeCollection(Enumerable.Empty<GenericAttribute>(), entityName, entityId, storeId);
            }

            // TODO: (core) Check if indexing the StoreId field makes things faster
            var query = from attr in _db.GenericAttributes
                        where
                            attr.EntityId == entityId && attr.KeyGroup == entityName &&
                            (attr.StoreId == storeId || attr.StoreId == 0)
                        select attr;

            var attrs = await query.ToListAsync();

            return new GenericAttributeCollection(attrs, entityName, entityId, storeId);
        }

        public virtual TProp GetAttribute<TProp>(string entityName, int entityId, string key, int storeId = 0)
        {
            return default;
        }

        public virtual Task<TProp> GetAttributeAsync<TProp>(string entityName, int entityId, string key, int storeId = 0)
        {
            return Task.FromResult(default(TProp));
        }

        public virtual void ApplyAttribute<TProp>(int entityId, string key, string keyGroup, TProp value, int storeId = 0)
        {
            // ...
        }

        public virtual Task ApplyAttributeAsync<TProp>(int entityId, string key, string keyGroup, TProp value, int storeId = 0)
        {
            return Task.CompletedTask;
        }
    }
}
