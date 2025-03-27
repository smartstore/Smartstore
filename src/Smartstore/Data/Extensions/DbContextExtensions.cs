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
    public static partial class DbContextExtensions
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
        /// If another instance of this entity with the same primary key is already attached,
        /// it will be detached, but only if it is in unchanged or deleted state.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">The entity instance</param>
        /// <returns><c>true</c> if the state of <paramref name="entity"/> has been changed, <c>false</c> if <paramref name="entity"/> is already attached.</returns>
        public static bool TryUpdate<TEntity>(this DbContext ctx, TEntity entity) where TEntity : BaseEntity
        {
            var detectChanges = ctx.ChangeTracker.AutoDetectChangesEnabled;
            ctx.ChangeTracker.AutoDetectChangesEnabled = false;

            using (new ActionDisposable(() => ctx.ChangeTracker.AutoDetectChangesEnabled = detectChanges))
            {
                // (perf) Turning off AutoDetectChangesEnabled prevents that ctx.Entry() performs change detection internally.
                var entry = ctx.Entry(entity);
                if (entry.State == EfState.Detached)
                {
                    TryDetachAlreadyTrackedEntity(ctx, entity);

                    entry.State = EfState.Unchanged;
                    entry.State = EfState.Modified;

                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Changes the state of an entity object when the requested state is different.
        /// If another instance of this entity with the same primary key is already attached,
        /// the attached entityis detached, but only if it is in unchanged or deleted state.
        /// </summary>
        /// <typeparam name="TEntity">Type of entity</typeparam>
        /// <param name="entity">The entity instance</param>
        /// <param name="requestedState">The requested new state</param>
        /// <returns><c>true</c> if the state has been changed, <c>false</c> if current state did not differ from <paramref name="requestedState"/>.
        /// </returns>
        public static bool TryChangeState<TEntity>(this DbContext ctx, TEntity entity, EfState requestedState) where TEntity : BaseEntity
        {
            var detectChanges = ctx.ChangeTracker.AutoDetectChangesEnabled;
            ctx.ChangeTracker.AutoDetectChangesEnabled = false;

            using (new ActionDisposable(() => ctx.ChangeTracker.AutoDetectChangesEnabled = detectChanges))
            {
                // (perf) Turning off AutoDetectChangesEnabled prevents that ctx.Entry() performs change detection internally.
                var entry = ctx.Entry(entity);
                if (entry.State != requestedState)
                {
                    // Only change state when requested state differs,
                    // because EF internally sets all properties to modified
                    // if necessary, even when requested state equals current state.

                    if (entry.State == EfState.Detached)
                    {
                        TryDetachAlreadyTrackedEntity(ctx, entity);
                    }
                    
                    entry.State = requestedState;
                    return true;
                }

                return false;
            }
        }

        private static bool TryDetachAlreadyTrackedEntity<TEntity>(DbContext ctx, TEntity entity) where TEntity : BaseEntity
        {
            // Attaching an entity while another instance with same primary key is attached will throw.
            // First we gonna try to locate an already attached entity...
            var attachedEntry = ctx.Set<TEntity>().Local.FindEntry(entity.Id);
            if (attachedEntry != null)
            {
                if (attachedEntry.State == EfState.Unchanged || attachedEntry.State == EfState.Deleted)
                {
                    // ...and detach it, but only Unchanged or Deleted entities and let others throw later.
                    attachedEntry.State = EfState.Detached;
                    return true;
                }
            }

            return false;
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
            Guard.NotNull(ctx);

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

                HashSet<BaseEntity> objSet = deep ? [] : null;

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
                    return !unchangedEntitiesOnly || entry.State == EfState.Unchanged;
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
    }
}
