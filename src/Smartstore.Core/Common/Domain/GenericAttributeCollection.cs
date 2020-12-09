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
        protected internal override SmartDbContext DbContext => _innerCollection.DbContext;
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

        internal GenericAttributeCollection(
            IQueryable<GenericAttribute> query, 
            string entityName, 
            int entityId, 
            int currentStoreId,
            List<GenericAttribute> entities = null)
        {
            Guard.NotNull(query, nameof(query));
            Guard.NotEmpty(entityName, nameof(entityName));

            Query = query;
            DbContext = query.GetDbContext<SmartDbContext>();
            EntityName = entityName;
            EntityId = entityId;
            CurrentStoreId = currentStoreId;
            Entities = entities;
            Map = new Multimap<string, GenericAttribute>(StringComparer.OrdinalIgnoreCase);

            if (Entities != null)
            {
                CreateMap();
            }
        }

        protected internal virtual List<GenericAttribute> Entities { get; set; }
        protected internal virtual SmartDbContext DbContext { get; }

        public virtual IQueryable<GenericAttribute> Query { get; }
        public virtual string EntityName { get; }
        public virtual int EntityId { get; }
        public virtual int CurrentStoreId { get; }

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

        #region Load/Save/Delete data

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

        public void DeleteAll()
        {
            EnsureLoaded();
            DbContext.GenericAttributes.RemoveRange(Entities);
            Entities.Clear();
            Map.Clear();
        }

        public int SaveChanges()
        {
            return DbContext.SaveChanges();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancelToken = default)
        {
            return DbContext.SaveChangesAsync(cancelToken);
        }

        private void CreateMap()
        {
            Map.Clear();
            foreach (var attr in Entities.OrderByDescending(x => x.StoreId))
            {
                Map.Add(attr.Key, attr);
            }
        }

        #endregion

        #region Access data

        /// <summary>
        /// Gets a generic attribute value
        /// </summary>
        /// <typeparam name="TProp">The type to convert raw <see cref="GenericAttribute.Value"/> to.</typeparam>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier; pass 0 to get a store-neutral attribute value.</param>
        /// <returns>Converted generic attribute value</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TProp Get<TProp>(string key, int storeId = 0)
        {
            if (TryGetEntity(key, storeId, out var entity))
            {
                return entity.Value.Convert<TProp>();
            }

            return default;
        }

        /// <summary>
        /// Tries to get a generic attribute entity.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="storeId">Store identifier; pass 0 to get a store-neutral entity.</param>
        /// <param name="entity">The underlying entity instance.</param>
        /// <returns><c>true</c> if an entity with given key is present, <c>false</c> otherwise</returns>
        public bool TryGetEntity(string key, int storeId, out GenericAttribute entity)
        {
            Guard.NotEmpty(key, nameof(key));

            EnsureLoaded();

            entity = null;

            if (Map.ContainsKey(key))
            {
                entity = Map[key].FirstOrDefault(x => x.StoreId == storeId);
                return entity != null;
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set<TProp>(string key, TProp value, int storeId = 0)
        {
            TrySet(key, value, storeId, out _);
        }

        public bool TrySet<TProp>(string key, TProp value, int storeId, out GenericAttribute entity)
        {
            Guard.NotEmpty(key, nameof(key));

            var valueStr = value.Convert<string>();

            EnsureLoaded();
            TryGetEntity(key, storeId, out entity);

            if (entity != null)
            {
                if (valueStr.HasValue())
                {
                    // Delete from Db
                    DbContext.GenericAttributes.Remove(entity);

                    // Delete from local map
                    Entities.Remove(entity);
                    Map.Remove(key, entity);
                }
                else
                {
                    entity.Value = valueStr;
                }
            }
            else
            {
                if (valueStr.HasValue())
                {
                    // The entity could have been deleted without saving a while ago.
                    // In this case we have to restore the deleted entry.
                    var deletedEntry = DbContext.ChangeTracker
                        .Entries<GenericAttribute>()
                        .Where(x => x.State == EntityState.Deleted)
                        .FirstOrDefault(x => x.Entity.EntityId == EntityId && x.Entity.Key.EqualsNoCase(key) && x.Entity.KeyGroup.EqualsNoCase(EntityName));

                    if (deletedEntry == null)
                    {
                        // Insert
                        entity = new GenericAttribute
                        {
                            KeyGroup = EntityName,
                            EntityId = EntityId,
                            Key = key,
                            StoreId = storeId,
                            Value = valueStr
                        };

                        // To DB
                        DbContext.GenericAttributes.Add(entity);
                    }
                    else
                    {
                        // Restore deleted entry
                        deletedEntry.State = EntityState.Modified;
                        entity = deletedEntry.Entity;
                        entity.Value = valueStr;
                    }

                    // Add to local map
                    Entities.Add(entity);
                    Map.Add(key, entity);
                }
            }

            return entity != null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureLoaded()
        {
            if (Entities == null)
            {
                Reload();
            }
        }

        #endregion
    }
}