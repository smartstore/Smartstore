using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Domain;

namespace Smartstore.Core.Common
{
    /// <summary>
    /// Generic attribute collection base implementation for covariance or <typeparamref name="TEntity"/> specific extension methods.
    /// </summary>
    public class GenericAttributeCollection<TEntity> : GenericAttributeCollection
        where TEntity : BaseEntity
    {
        private readonly GenericAttributeCollection _innerCollection;

        public GenericAttributeCollection(GenericAttributeCollection innerCollection)
            : base()
        {
            Guard.NotNull(innerCollection, nameof(innerCollection));

            _innerCollection = innerCollection;
        }

        public override IQueryable<GenericAttribute> Query => _innerCollection.Query;
        public override string EntityName => _innerCollection.EntityName;
        public override int EntityId => _innerCollection.EntityId;
        public override int CurrentStoreId => _innerCollection.CurrentStoreId;
        protected internal override Multimap<string, GenericAttribute> Map => _innerCollection.Map;

        protected internal override List<GenericAttribute> Entities 
        { 
            get => _innerCollection.Entities; 
            set => _innerCollection.Entities = value; 
        }
    }

    public class GenericAttributeCollection
    {
        protected GenericAttributeCollection()
        {
        }

        internal GenericAttributeCollection(IQueryable<GenericAttribute> query, string entityName, int entityId, int currentStoreId)
        {
            Guard.NotNull(query, nameof(query));

            Query = query;
            EntityName = entityName;
            EntityId = entityId;
            CurrentStoreId = currentStoreId;
            Map = new Multimap<string, GenericAttribute>(StringComparer.OrdinalIgnoreCase);
        }

        public virtual IQueryable<GenericAttribute> Query { get; }
        public virtual string EntityName { get; }
        public virtual int EntityId { get; }
        public virtual int CurrentStoreId { get; }
        protected internal virtual List<GenericAttribute> Entities { get; set; }

        // Key: GenericAttribute.Key
        protected internal virtual Multimap<string, GenericAttribute> Map { get; }

        public IEnumerable<GenericAttribute> UnderlyingEntities
        {
            get
            {
                if (Entities == null)
                {
                    Reload();
                }

                return Entities;
            }
        }

        #region Load/Save data

        private void CreateMap()
        {
            Map.Clear();
            foreach (var ga in Entities.OrderByDescending(x => x.StoreId))
            {
                Map.Add(ga.Key, ga);
            }
        }

        public void Reload()
        {
            Entities = Query.ToList();
            CreateMap();
        }

        public async Task ReloadAsync()
        {
            Entities = await Query.ToListAsync();
            CreateMap();
        }

        public void Reset()
        {
            Entities = null;
            Map.Clear();
        }

        public int SaveChanges()
        {
            return Query.GetDbContext<SmartDbContext>().SaveChanges();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancelToken = default)
        {
            return Query.GetDbContext<SmartDbContext>().SaveChangesAsync(cancelToken);
        }

        #endregion

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

            if (Map.ContainsKey(key))
            {
                value = Map[key].FirstOrDefault().Convert<TProp>();
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
