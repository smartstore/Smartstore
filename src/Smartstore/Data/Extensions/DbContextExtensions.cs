using System.Data;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Smartstore.ComponentModel;
using Smartstore.Data;
using Smartstore.Domain;
using Smartstore.Utilities;

namespace Smartstore
{
    public static class DbContextExtensions
    {
        #region Connection

        /// <summary>
        /// Opens and retains connection until end of scope. Call this method in long running 
        /// processes to gain slightly faster database interaction.
        /// </summary>
        /// <param name="ctx">The database context</param>
        public static IDisposable OpenConnection(this DbContext ctx)
        {
            bool wasOpened = false;
            var db = ctx.Database;

            if (db.GetDbConnection().State != ConnectionState.Open)
            {
                db.OpenConnection();
                wasOpened = true;
            }

            return new ActionDisposable(() =>
            {
                if (wasOpened)
                    db.CloseConnection();
            });
        }

        /// <summary>
        /// Opens and retains connection until end of scope. Call this method in long running 
        /// processes to gain slightly faster database interaction.
        /// </summary>
        /// <param name="ctx">The database context</param>
        public static async Task<IAsyncDisposable> OpenConnectionAsync(this DbContext ctx)
        {
            bool wasOpened = false;
            var db = ctx.Database;

            if (db.GetDbConnection().State != ConnectionState.Open)
            {
                await db.OpenConnectionAsync();
                wasOpened = true;
            }

            return new AsyncActionDisposable(async () =>
            {
                if (wasOpened)
                    await db.CloseConnectionAsync();
            });
        }

        #endregion

        #region Entity states, detaching

        /// <summary>
        /// Tries to locate an already loaded and tracked entity in the local state manager.
        /// </summary>
        /// <typeparam name="TEntity">The type of entity to locate.</typeparam>
        /// <param name="entityId">The primary key of entity to locate.</param>
        /// <returns>The entity instance if found, <c>null</c> otherwise.</returns>
        public static TEntity FindTracked<TEntity>(this HookingDbContext ctx, int entityId)
            where TEntity : BaseEntity
        {
            return ctx.Set<TEntity>().Local.FindEntry(entityId)?.Entity;
        }

        /// <summary>
        /// Sets the state of an entity to <see cref="EfState.Modified"/> if it is detached.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">The entity instance</param>
        /// <returns><c>true</c> if the state has been changed, <c>false</c> if entity is attached already.</returns>
        public static bool TryUpdate<TEntity>(this DbContext ctx, TEntity entity) where TEntity : BaseEntity
        {
            var detectChanges = ctx.ChangeTracker.AutoDetectChangesEnabled;
            ctx.ChangeTracker.AutoDetectChangesEnabled = false;

            using (new ActionDisposable(() => ctx.ChangeTracker.AutoDetectChangesEnabled = detectChanges))
            {
                // (perf) turning off AutoDetectChangesEnabled prevents that ctx.Entry() performs change detection internally.
                var entry = ctx.Entry(entity);
                return entry.TryUpdate();
            }
        }

        /// <summary>
        /// Changes the state of an entity object when requested state differs.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">The entity instance</param>
        /// <param name="requestedState">The requested new state</param>
        /// <returns><c>true</c> if the state has been changed, <c>false</c> if current state did not differ from <paramref name="requestedState"/>.</returns>
        public static bool TryChangeState<TEntity>(this DbContext ctx, TEntity entity, EfState requestedState) where TEntity : BaseEntity
        {
            var detectChanges = ctx.ChangeTracker.AutoDetectChangesEnabled;
            ctx.ChangeTracker.AutoDetectChangesEnabled = false;

            using (new ActionDisposable(() => ctx.ChangeTracker.AutoDetectChangesEnabled = detectChanges))
            {
                // (perf) turning off autoDetectChanges prevents that ctx.Entry() performs change detection internally.
                var entry = ctx.Entry(entity);
                return entry.TryChangeState(requestedState);
            }
        }

        /// <summary>
        /// Determines whether an entity property has changed since it was attached.
        /// </summary>
        /// <param name="entity">Entity</param>
        /// <param name="propertyName">The property name to check</param>
        /// <param name="originalValue">The previous/original property value if change was detected</param>
        /// <returns><c>true</c> if property has changed, <c>false</c> otherwise</returns>
        public static bool TryGetModifiedProperty(this HookingDbContext ctx, BaseEntity entity, string propertyName, out object originalValue)
        {
            Guard.NotNull(entity);

            if (entity.IsTransientRecord())
            {
                originalValue = null;
                return false;
            }

            var entry = ctx.Entry((object)entity);
            return entry.TryGetModifiedProperty(propertyName, out originalValue);
        }

        /// <summary>
        /// Gets a list of modified properties for the specified entity
        /// </summary>
        /// <param name="entity">The entity instance for which to get modified properties for</param>
        /// <returns>
        /// A dictionary, where the key is the name of the modified property
        /// and the value is its ORIGINAL value (which was tracked when the entity
        /// was attached to the context the first time)
        /// Returns an empty dictionary if no modification could be detected.
        /// </returns>
        public static IDictionary<string, object> GetModifiedProperties(this HookingDbContext ctx, BaseEntity entity)
        {
            return ctx.Entry((object)entity).GetModifiedProperties();
        }

        /// <summary>
        /// Reloads the entity from the database overwriting any property values with values from the database. 
        /// The entity will be in the Unchanged state after calling this method. 
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">The entity instance</param>
        public static void ReloadEntity<TEntity>(this DbContext ctx, TEntity entity) where TEntity : BaseEntity
        {
            ctx.Entry((object)entity).ReloadEntity();
        }

        /// <summary>
        /// Reloads the entity from the database overwriting any property values with values from the database. 
        /// The entity will be in the Unchanged state after calling this method. 
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">The entity instance</param>
        public static Task ReloadEntityAsync<TEntity>(this DbContext ctx, TEntity entity, CancellationToken cancelToken = default) where TEntity : BaseEntity
        {
            return ctx.Entry((object)entity).ReloadEntityAsync(cancelToken);
        }

        /// <summary>
        /// Detaches an entity from the current context if it's attached.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">The entity instance to detach</param>
        /// <param name="deep">Whether to scan all navigation properties and detach them recursively also.</param>
        /// <returns>The count of detached entities</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DetachEntity<TEntity>(this HookingDbContext ctx, TEntity entity, bool deep = false) where TEntity : BaseEntity
        {
            return ctx.DetachInternal(entity, deep ? new HashSet<BaseEntity>() : null, deep);
        }

        /// <summary>
        /// Detaches all passed entities from the current context.
        /// </summary>
        /// <param name="unchangedEntitiesOnly">When <c>true</c>, only entities in unchanged state get detached.</param>
        /// <param name="deep">Whether to scan all navigation properties and detach them recursively also. LazyLoading should be turned off when <c>true</c>.</param>
        /// <returns>The count of detached entities</returns>
        public static void DetachEntities<TEntity>(this HookingDbContext ctx, IEnumerable<TEntity> entities, bool deep = false) where TEntity : BaseEntity
        {
            Guard.NotNull(ctx, nameof(ctx));

            using (new DbContextScope(ctx, autoDetectChanges: false, lazyLoading: false))
            {
                entities.Each(x => ctx.DetachEntity(x, deep));
            }
        }

        /// <summary>
        /// Detaches all entities of type <c>TEntity</c> from the current object context
        /// </summary>
        /// <param name="unchangedEntitiesOnly">When <c>true</c>, only entities in unchanged state get detached.</param>
        /// <param name="deep">Whether to scan all navigation properties and detach them recursively also. LazyLoading should be turned off when <c>true</c>.</param>
        /// <returns>The count of detached entities</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DetachEntities<TEntity>(this HookingDbContext ctx, bool unchangedEntitiesOnly = true, bool deep = false) where TEntity : BaseEntity
        {
            return ctx.DetachEntities(o => o is TEntity, unchangedEntitiesOnly, deep);
        }

        /// <summary>
        /// Detaches all entities matching the passed <paramref name="predicate"/> from the current object context
        /// </summary>
        /// <param name="unchangedEntitiesOnly">When <c>true</c>, only entities in unchanged state will be detached.</param>
        /// <param name="deep">Whether to scan all navigation properties and detach them recursively also.</param>
        /// <returns>The count of detached entities</returns>
        public static int DetachEntities(this HookingDbContext ctx, Func<BaseEntity, bool> predicate, bool unchangedEntitiesOnly = true, bool deep = false)
        {
            Guard.NotNull(predicate);

            var numDetached = 0;

            using (new DbContextScope(ctx, autoDetectChanges: false, lazyLoading: false))
            {
                var entries = ctx.ChangeTracker.Entries<BaseEntity>().Where(Match).ToList();

                HashSet<BaseEntity> objSet = deep ? new HashSet<BaseEntity>() : null;

                foreach (var entry in entries)
                {
                    numDetached += ctx.DetachInternal(entry, objSet, deep);
                }

                return numDetached;
            }

            bool Match(EntityEntry<BaseEntity> entry)
            {
                if (entry.State > EfState.Detached && predicate(entry.Entity))
                {
                    return unchangedEntitiesOnly
                        ? entry.State == EfState.Unchanged
                        : true;
                }

                return false;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int DetachInternal(this HookingDbContext ctx, BaseEntity obj, ISet<BaseEntity> objSet, bool deep)
        {
            if (obj == null)
                return 0;

            return ctx.DetachInternal(ctx.Entry(obj), objSet, deep);
        }

        private static int DetachInternal(this HookingDbContext ctx, EntityEntry<BaseEntity> entry, ISet<BaseEntity> objSet, bool deep)
        {
            var obj = entry.Entity;
            int numDetached = 0;

            if (deep)
            {
                // This is to prevent an infinite recursion when the child object has a navigation property
                // that points back to the parent
                if (objSet != null && !objSet.Add(obj))
                    return 0;

                // Recursively detach all navigation properties
                foreach (var prop in FastProperty.GetProperties(obj.GetType()).Values)
                {
                    if (typeof(BaseEntity).IsAssignableFrom(prop.Property.PropertyType))
                    {
                        numDetached += ctx.DetachInternal(prop.GetValue(obj) as BaseEntity, objSet, deep);
                    }
                    else if (typeof(IEnumerable<BaseEntity>).IsAssignableFrom(prop.Property.PropertyType))
                    {
                        var val = prop.GetValue(obj);
                        if (val is IEnumerable<BaseEntity> list)
                        {
                            foreach (var item in list.ToList())
                            {
                                numDetached += ctx.DetachInternal(item, objSet, deep);
                            }
                        }
                    }
                }
            }

            entry.State = EfState.Detached;
            numDetached++;

            return numDetached;
        }

        #endregion

        #region Load collection / reference

        /// <summary>
        /// Checks whether a collection type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsCollectionLoaded<TEntity, TCollection>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            return IsCollectionLoaded(ctx, entity, navigationProperty, out _);
        }

        /// <summary>
        /// Checks whether a collection type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        public static bool IsCollectionLoaded<TEntity, TCollection>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty,
            out CollectionEntry<TEntity, TCollection> collectionEntry)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            Guard.NotNull(entity);
            Guard.NotNull(navigationProperty);

            collectionEntry = null;
            if (entity.Id == 0)
            {
                return false;
            }

            var entry = ctx.Entry(entity);
            collectionEntry = entry.Collection(navigationProperty);
            var isLoaded = collectionEntry.CurrentValue != null || collectionEntry.IsLoaded;

            // Avoid System.InvalidOperationException: Member 'IsLoaded' cannot be called for property...
            if (!isLoaded && (entry.State == EfState.Detached))
            {
                // Attaching an entity while another instance with same primary key is attached will throw.
                // First we gonna try to locate an already attached entity.
                var other = ctx.FindTracked<TEntity>(entity.Id);
                if (other != null)
                {
                    collectionEntry = ctx.Entry(entity).Collection(navigationProperty);
                }
                else
                {
                    ctx.Attach(entity);
                }
            }

            return isLoaded;
        }

        /// <summary>
        /// Checks whether a reference type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsReferenceLoaded<TEntity, TProperty>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            return IsReferenceLoaded(ctx, entity, navigationProperty, out _);
        }

        /// <summary>
        /// Checks whether a reference type navigation property has already 
        /// been loaded for a given entity (either eagerly or lazily).
        /// </summary>
        public static bool IsReferenceLoaded<TEntity, TProperty>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty,
            out ReferenceEntry<TEntity, TProperty> referenceEntry)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            Guard.NotNull(entity, nameof(entity));
            Guard.NotNull(navigationProperty, nameof(navigationProperty));

            referenceEntry = null;
            if (entity.Id == 0)
            {
                return false;
            }

            var entry = ctx.Entry(entity);
            referenceEntry = entry.Reference(navigationProperty);
            var isLoaded = referenceEntry.CurrentValue != null || referenceEntry.IsLoaded;

            // Avoid System.InvalidOperationException: Member 'IsLoaded' cannot be called for property...
            if (!isLoaded && (entry.State == EfState.Detached))
            {
                // Attaching an entity while another instance with same primary key is attached will throw.
                // First we gonna try to locate an already attached entity.
                var other = ctx.FindTracked<TEntity>(entity.Id);
                if (other != null)
                {
                    referenceEntry = ctx.Entry(entity).Reference(navigationProperty);
                }
                else
                {
                    ctx.Attach(entity);
                }
            }

            return isLoaded;
        }

        /// <summary>
        /// Loads entities referenced by a collection navigation property from database, unless data is already loaded.
        /// </summary>
        /// <param name="entity">Entity instance to load data for.</param>
        /// <param name="navigationProperty">The navigation property expression.</param>
        /// <param name="force"><c>false:</c> do nothing if data is already loaded. <c>true:</c> reload data even if already loaded.</param>
        /// <param name="queryModifier">Modifier for the query that is about to be executed against the database.</param>
        /// <param name="cancelToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        public static async Task<CollectionEntry<TEntity, TCollection>> LoadCollectionAsync<TEntity, TCollection>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, IEnumerable<TCollection>>> navigationProperty,
            bool force = false,
            Func<IQueryable<TCollection>, IQueryable<TCollection>> queryModifier = null,
            CancellationToken cancelToken = default)
            where TEntity : BaseEntity
            where TCollection : BaseEntity
        {
            Guard.NotNull(entity);
            Guard.NotNull(navigationProperty);

            if (entity.Id == 0)
            {
                return null;
            }

            var entry = ctx.Entry(entity);
            if (entry.State == EfState.Deleted)
            {
                return null;
            }

            var collection = entry.Collection(navigationProperty);
            // TODO: (core) Entities with hashSets as collections always return true here, as they are never null (for example: Product.ProductVariantAttributes).
            var isLoaded = collection.CurrentValue != null || collection.IsLoaded;

            if (!isLoaded && entry.State == EfState.Detached)
            {
                try
                {
                    // Attaching an entity while another instance with same primary key is attached will throw.
                    // First we gonna try to locate an already attached entity.
                    var other = ctx.FindTracked<TEntity>(entity.Id);
                    if (other != null)
                    {
                        // An entity with same key is attached already. So we gonna load the navigation property of attached entity
                        // and copy the result to this detached entity. This way we don't need to attach the source entity.
                        var otherCollection = await ctx.LoadCollectionAsync(other, navigationProperty, force, queryModifier, cancelToken: cancelToken);

                        // Copy collection over to detached entity.
                        collection.CurrentValue = otherCollection.CurrentValue;
                        collection.IsLoaded = true;
                        isLoaded = true;
                        force = false;
                    }
                    else
                    {
                        ctx.Attach(entity);
                    }
                }
                catch
                {
                    // Attach may throw!
                }
            }

            if (force)
            {
                collection.IsLoaded = false;
                isLoaded = false;
            }

            if (!isLoaded)
            {
                if (queryModifier != null)
                {
                    var query = queryModifier(collection.Query());
                    collection.CurrentValue = await query.ToListAsync(cancellationToken: cancelToken);
                }
                else
                {
                    await collection.LoadAsync(cancelToken);
                }

                collection.IsLoaded = true;
            }

            return collection;
        }

        /// <summary>
        /// Loads an entity referenced by a navigation property from database, unless data is already loaded.
        /// </summary>
        /// <param name="entity">Entity instance to load data for.</param>
        /// <param name="navigationProperty">The navigation property expression.</param>
        /// <param name="force"><c>false:</c> do nothing if data is already loaded. <c>true:</c> Reload data event if loaded already.</param>
        /// <param name="queryModifier">Modifier for the query that is about to be executed against the database.</param>
        /// <param name="cancelToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
        public static async Task<ReferenceEntry<TEntity, TProperty>> LoadReferenceAsync<TEntity, TProperty>(
            this HookingDbContext ctx,
            TEntity entity,
            Expression<Func<TEntity, TProperty>> navigationProperty,
            bool force = false,
            Func<IQueryable<TProperty>, IQueryable<TProperty>> queryModifier = null,
            CancellationToken cancelToken = default)
            where TEntity : BaseEntity
            where TProperty : BaseEntity
        {
            Guard.NotNull(entity);
            Guard.NotNull(navigationProperty);

            if (entity.Id == 0)
            {
                return null;
            }

            var entry = ctx.Entry(entity);
            if (entry.State == EfState.Deleted)
            {
                return null;
            }

            var reference = entry.Reference(navigationProperty);
            var isLoaded = reference.CurrentValue != null || reference.IsLoaded;

            if (!isLoaded && entry.State == EfState.Detached)
            {
                try
                {
                    // Attaching an entity while another instance with same primary key is attached will throw.
                    // First we gonna try to locate an already attached entity.
                    var other = ctx.FindTracked<TEntity>(entity.Id);
                    if (other != null)
                    {
                        // An entity with same key is attached already. So we gonna load the reference property of attached entity
                        // and copy the result to this detached entity. This way we don't need to attach the source entity.
                        var otherReference = await ctx.LoadReferenceAsync(other, navigationProperty, force, queryModifier, cancelToken: cancelToken);

                        // Copy reference over to detached entity.
                        reference.CurrentValue = otherReference.CurrentValue;
                        reference.IsLoaded = true;
                        isLoaded = true;
                        force = false;
                    }
                    else
                    {
                        ctx.Attach(entity);
                    }
                }
                catch
                {
                    // Attach may throw!
                }
            }

            if (force)
            {
                reference.IsLoaded = false;
                isLoaded = false;
            }

            if (!isLoaded)
            {
                if (queryModifier != null)
                {
                    var query = queryModifier(reference.Query());
                    reference.CurrentValue = await query.FirstOrDefaultAsync(cancellationToken: cancelToken);
                }
                else
                {
                    await reference.LoadAsync(cancelToken);
                }

                reference.IsLoaded = true;
            }

            return reference;
        }

        #endregion
    }
}
