using System.Runtime.CompilerServices;
using Smartstore.Collections;
using Smartstore.Core.Data;

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
            IsReadOnly = innerCollection.IsReadOnly;
        }

        public override string EntityName => _innerCollection.EntityName;
        public override int EntityId => _innerCollection.EntityId;
        public override int CurrentStoreId => _innerCollection.CurrentStoreId;

        protected internal override IQueryable<GenericAttribute> Query => _innerCollection.Query;
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

            IsReadOnly = false;
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

        /// <summary>
        /// For transient entities and to avoid that MVC model binder crashes.
        /// </summary>
        /// <param name="entityName"></param>
        internal GenericAttributeCollection(string entityName)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            IsReadOnly = true;
            EntityName = entityName;
        }

        public bool IsReadOnly { get; protected set; }

        public virtual string EntityName { get; }
        public virtual int EntityId { get; }
        public virtual int CurrentStoreId { get; }

        protected internal virtual List<GenericAttribute> Entities { get; set; }
        protected internal virtual SmartDbContext DbContext { get; }
        protected internal virtual IQueryable<GenericAttribute> Query { get; }

        // Key: GenericAttribute.Key
        protected internal virtual Multimap<string, GenericAttribute> Map { get; }

        private void CheckNotReadonly()
        {
            if (IsReadOnly)
            {
                throw new NotSupportedException("Collection is read-only.");
            }
        }

        /// <summary>
        /// Gets all entities that were loaded from the database
        /// </summary>
        public IEnumerable<GenericAttribute> UnderlyingEntities
        {
            get
            {
                if (IsReadOnly)
                {
                    return Enumerable.Empty<GenericAttribute>();
                }
                
                if (Entities == null)
                {
                    Reload();
                }

                return Entities;
            }
        }

        #region Load/Save/Delete data

        /// <summary>
        /// Reloads all entities from database
        /// </summary>
        public void Reload()
        {
            if (!IsReadOnly)
            {
                Entities = Query.ToList();
                CreateMap();
            }
        }

        /// <summary>
        /// Reloads all entities from database
        /// </summary>
        public async Task ReloadAsync()
        {
            if (!IsReadOnly)
            {
                Entities = await Query.ToListAsync();
                CreateMap();
            }
        }

        /// <summary>
        /// Marks all underlying entities as deleted. This method is non-saving.
        /// </summary>
        public void DeleteAll()
        {
            CheckNotReadonly();
            EnsureLoaded();
            DbContext.GenericAttributes.RemoveRange(Entities);
            Entities.Clear();
            Map.Clear();
        }

        /// <summary>
        /// Saves all entity changes to the database.
        /// </summary>
        /// <returns>Number of affected records.</returns>
        public int SaveChanges()
        {
            CheckNotReadonly();
            return DbContext.SaveChanges();
        }

        /// <summary>
        /// Saves all entity changes to the database.
        /// </summary>
        /// <returns>Number of affected records.</returns>
        public Task<int> SaveChangesAsync(CancellationToken cancelToken = default)
        {
            CheckNotReadonly();
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

            entity = null;

            if (IsReadOnly)
            {
                return false;
            }

            EnsureLoaded();

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

            CheckNotReadonly();

            var valueStr = value.Convert<string>();

            EnsureLoaded();
            TryGetEntity(key, storeId, out entity);

            if (entity != null)
            {
                if (valueStr.IsEmpty())
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
                        .Where(x => x.State == EfState.Deleted)
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
                        deletedEntry.State = EfState.Modified;
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