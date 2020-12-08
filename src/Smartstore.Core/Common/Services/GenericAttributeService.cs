using System;
using System.Collections.Generic;
using System.Linq;
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

        // Key = (EntityName, EntityId)
        private readonly Dictionary<(string, int), GenericAttributeCollection> _collectionCache = new();

        public GenericAttributeService(SmartDbContext db, IStoreContext storeContext)
        {
            _db = db;
            _storeContext = storeContext;
        }

        public virtual GenericAttributeCollection GetAttributesForEntity(string entityName, int entityId)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            if (entityId <= 0)
            {
                return null;
            }

            var key = (entityName.ToLowerInvariant(), entityId);

            if (!_collectionCache.TryGetValue(key, out var collection))
            {
                var query = from attr in _db.GenericAttributes
                            where attr.EntityId == entityId && attr.KeyGroup == entityName
                            select attr;

                collection = new GenericAttributeCollection(query, entityName, entityId, _storeContext.CurrentStore.Id);
                _collectionCache[key] = collection;
            }

            return collection;
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
