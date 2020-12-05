using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Smartstore.Collections;

namespace Smartstore.Core.Common
{
    public class GenericAttributeCollection
    {
        // Key: GenericAttribute.Key
        private Multimap<string, GenericAttribute> _map = new();
        
        public GenericAttributeCollection(GenericAttributeCollection collection)
        {
            Guard.NotNull(collection, nameof(collection));

            UnderlyingEntities = collection.UnderlyingEntities;
            EntityName = collection.EntityName;
            EntityId = collection.EntityId;
            StoreId = collection.StoreId;

            CreateMap();
        }

        public GenericAttributeCollection(IEnumerable<GenericAttribute> entities, string entityName, int entityId, int storeId)
        {
            Guard.NotNull(entities, nameof(entities));

            UnderlyingEntities = entities;
            EntityName = entityName;
            EntityId = entityId;
            StoreId = storeId;

            CreateMap();
        }

        private void CreateMap()
        {
            _map.Clear();
            foreach (var ga in UnderlyingEntities.OrderByDescending(x => x.StoreId))
            {
                _map.Add(ga.Key, ga);
            }
        }

        public IEnumerable<GenericAttribute> UnderlyingEntities { get; private set; }
        public string EntityName { get; private set; }
        public int EntityId { get; private set; }
        public int StoreId { get; private set; }

        #region Access data

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TProp Get<TProp>(string key)
        {
            TryGet<TProp>(key, out var value);
            return value;
        }

        public bool TryGet<TProp>(string key, out TProp value)
        {
            value = default;

            if (_map.ContainsKey(key))
            {
                value = _map[key].FirstOrDefault().Convert<TProp>();
                return true;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<TProp>(string key, TProp value)
        {
            //
        }

        public bool TrySet<TProp>(string key, TProp value, out GenericAttribute entity)
        {
            entity = null;

            //if (_map.ContainsKey(key))
            //{
            //    entity = _map[key].FirstOrDefault();
            //    if (entity != null)
            //    {
            //        var valueStr = value.Convert<string>();
            //    }
            //}

            return entity != null;
        }

        #endregion
    }
}
