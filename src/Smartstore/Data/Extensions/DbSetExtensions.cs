using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Data;
using Smartstore.Data.Hooks;
using Smartstore.Domain;

namespace Smartstore
{
    public static class DbSetExtensions
    {
        /// <summary>
        /// Resolves the <see cref="DbContext"/> instance from which the given
        /// <see cref="DbSet{TEntity}"/> was obtained.
        /// </summary>
        public static HookingDbContext GetDbContext<TEntity>(this DbSet<TEntity> dbSet)
            where TEntity : BaseEntity
        {
            Guard.NotNull(dbSet);

            var infrastructure = dbSet as IInfrastructure<IServiceProvider>;
            var serviceProvider = infrastructure.Instance;
            var currentDbContext = serviceProvider.GetService(typeof(ICurrentDbContext)) as ICurrentDbContext;

            return (HookingDbContext)currentDbContext.Context;
        }

        #region Find

        /// <summary>
        ///     Finds an entity with the given id. If an entity with the given id
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given id
        ///     and this entity, if found, is returned. If no entity is found, then
        ///     null is returned. If <paramref name="tracked"/> is <see langword="true"/>, 
        ///     then the entity is also attached to the context, so that subsequent calls can
        ///     return the tracked entity without a database roundtrip.
        /// </summary>
        /// <param name="id">The primary id of the entity.</param>
        /// <param name="tracked">
        ///     Whether to put entity to change tracker after it was loaded from database. Note that <c>false</c>
        ///     has no effect if the entity was in change tracker already (it will NOT be detached).
        /// </param>
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.Find(object[])"/>
        ///     because the key is known.
        /// </remarks>
        public static TEntity FindById<TEntity>(this DbSet<TEntity> dbSet, int id, bool tracked = true)
            where TEntity : BaseEntity
        {
            if (id == 0)
                return null;

            return dbSet.Local.FindEntry(id)?.Entity ?? dbSet.ApplyTracking(tracked).SingleOrDefault(x => x.Id == id);
        }

        /// <summary>
        ///     Finds an entity with the given id. If an entity with the given id
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given id
        ///     and this entity, if found, is returned. If no entity is found, then
        ///     null is returned. If <paramref name="tracked"/> is <see langword="true"/>, 
        ///     then the entity is also attached to the context, so that subsequent calls can
        ///     return the tracked entity without a database roundtrip.
        /// </summary>
        /// <param name="id">The primary id of the entity.</param>
        /// <param name="tracked">
        ///     Whether to put entity to change tracker after it was loaded from database. Note that <c>false</c>
        ///     has no effect if the entity was in change tracker already (it will NOT be detached).
        /// </param>
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.Find(object[])"/>
        ///     because the key is known.
        /// </remarks>
        public static TEntity FindById<TEntity, TProperty>(this IIncludableQueryable<TEntity, TProperty> query, int id, bool tracked = true)
            where TEntity : BaseEntity
        {
            if (id == 0)
                return null;
            
            return query.GetDbContext().Set<TEntity>().Local.FindEntry(id)?.Entity ?? query.ApplyTracking(tracked).SingleOrDefault(x => x.Id == id);
        }

        /// <summary>
        ///     Finds an entity with the given id. If an entity with the given id
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given id
        ///     and this entity, if found, is returned. If no entity is found, then
        ///     null is returned. If <paramref name="tracked"/> is <see langword="true"/>, 
        ///     then the entity is also attached to the context, so that subsequent calls can
        ///     return the tracked entity without a database roundtrip.
        /// </summary>
        /// <param name="id">The primary id of the entity.</param>
        /// <param name="tracked">
        ///     Whether to put entity to change tracker after it was loaded from database. Note that <c>false</c>
        ///     has no effect if the entity was in change tracker already (it will NOT be detached).
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.FindAsync(object[], CancellationToken)(object[])"/>
        ///     because the key is known.
        /// </remarks>
        public static ValueTask<TEntity> FindByIdAsync<TEntity>(this DbSet<TEntity> dbSet, int id, bool tracked = true, CancellationToken cancellationToken = default)
            where TEntity : BaseEntity
        {
            if (id == 0)
                return ValueTask.FromResult((TEntity)null);

            var trackedEntity = dbSet.Local.FindEntry(id)?.Entity;
            return trackedEntity != null
                ? new ValueTask<TEntity>(trackedEntity)
                : new ValueTask<TEntity>(
                    dbSet.ApplyTracking(tracked).SingleOrDefaultAsync(x => x.Id == id, cancellationToken));
        }

        /// <summary>
        ///     Finds an entity with the given id. If an entity with the given id
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given id
        ///     and this entity, if found, is returned. If no entity is found, then
        ///     null is returned. If <paramref name="tracked"/> is <see langword="true"/>, 
        ///     then the entity is also attached to the context, so that subsequent calls can
        ///     return the tracked entity without a database roundtrip.
        /// </summary>
        /// <param name="id">The primary id of the entity.</param>
        /// <param name="tracked">
        ///     Whether to put entity to change tracker after it was loaded from database. Note that <c>false</c>
        ///     has no effect if the entity was in change tracker already (it will NOT be detached).
        /// </param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.FindAsync(object[], CancellationToken)(object[])"/>
        ///     because the key is known.
        /// </remarks>
        public static ValueTask<TEntity> FindByIdAsync<TEntity, TProperty>(this IIncludableQueryable<TEntity, TProperty> query, int id, bool tracked = true, CancellationToken cancellationToken = default)
            where TEntity : BaseEntity
        {
            if (id == 0)
                return ValueTask.FromResult((TEntity)null);

            var trackedEntity = query.GetDbContext().Set<TEntity>().Local.FindEntry(id)?.Entity;
            return trackedEntity != null
                ? new ValueTask<TEntity>(trackedEntity)
                : new ValueTask<TEntity>(
                    query.ApplyTracking(tracked).SingleOrDefaultAsync(x => x.Id == id, cancellationToken));
        }

        #endregion

        #region Remove

        public static void Remove<TEntity>(this DbSet<TEntity> dbSet, int id) where TEntity : BaseEntity, new()
        {
            Guard.NotZero(id);

            var entity = dbSet.Local.FindEntry(id)?.Entity;
            if (entity == null && dbSet.Any(x => x.Id == id))
            {
                entity = new TEntity { Id = id };
            }

            if (entity != null)
            {
                dbSet.Remove(entity);
            }
        }

        public static void RemoveRange<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<int> ids) where TEntity : BaseEntity, new()
        {
            Guard.NotNull(ids);

            var distinctIds = ids.Where(id => id > 0).Distinct();
            if (!distinctIds.Any())
            {
                return;
            }

            var context = dbSet.GetDbContext();

            var localEntities = distinctIds
                .Select(context.FindTracked<TEntity>)
                .Where(x => x != null)
                .ToList();
            
            var untrackedIds = distinctIds.Except(localEntities.Select(x => x.Id)).ToArray();
            var dbEntities = dbSet
                .Where(x => untrackedIds.Contains(x.Id))
                .Select(x => new TEntity { Id = x.Id })
                .ToList();

            dbSet.RemoveRange(localEntities.Concat(dbEntities));
        }

        /// <summary>
        /// Truncates the table
        /// </summary>
        /// <typeparam name="TEntity">Entity type</typeparam>
        /// <param name="predicate">An optional filter</param>
        /// <param name="cascade">
        /// <c>false</c>: does not make any attempts to determine dependant entities, just deletes ONLY them (faster).
        /// <c>true</c>: loads all entities into the context first and deletes them, along with their dependencies (slower).
        /// </param>
        /// <returns>The total number of affected entities</returns>
        /// <remarks>
        /// This method turns off auto detection, validation and hooking.
        /// </remarks>
        public static int DeleteAll<TEntity>(this DbSet<TEntity> dbSet, Expression<Func<TEntity, bool>> predicate = null, bool cascade = false) where TEntity : BaseEntity, new()
        {
            var numDeleted = 0;
            var ctx = dbSet.GetDbContext();

            using (var scope = new DbContextScope(db: ctx, autoDetectChanges: false, minHookImportance: HookImportance.Important))
            {
                var query = dbSet.AsQueryable();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                if (cascade)
                {
                    var records = query.ToList();
                    foreach (var chunk in records.Chunk(500))
                    {
                        dbSet.RemoveRange(chunk);
                        numDeleted += ctx.SaveChanges();
                    }
                }
                else
                {
                    numDeleted = query.ExecuteDelete();
                }
            }

            return numDeleted;
        }

        /// <summary>
        /// Truncates the table
        /// </summary>
        /// <typeparam name="T">Entity type</typeparam>
        /// <param name="predicate">An optional filter</param>
        /// <param name="cascade">
        /// <c>false</c>: does not make any attempts to determine dependant entities, just deletes ONLY them (faster).
        /// <c>true</c>: loads all entities into the context first and deletes them, along with their dependencies (slower).
        /// </param>
        /// <returns>The total number of affected entities</returns>
        /// <remarks>
        /// This method turns off auto detection, validation and hooking.
        /// </remarks>
        public static async Task<int> DeleteAllAsync<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate = null, bool cascade = false) where T : BaseEntity, new()
        {
            var numDeleted = 0;
            var ctx = dbSet.GetDbContext();

            using (var scope = new DbContextScope(db: ctx, autoDetectChanges: false, minHookImportance: HookImportance.Important))
            {
                var query = dbSet.AsQueryable();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                if (cascade)
                {
                    var records = await query.ToListAsync();
                    foreach (var chunk in records.Chunk(500))
                    {
                        dbSet.RemoveRange(chunk);
                        numDeleted += await ctx.SaveChangesAsync();
                    }
                }
                else
                {
                    numDeleted = await query.ExecuteDeleteAsync();
                }
            }

            return numDeleted;
        }

        #endregion

        #region Aggregates

        /// <summary>
        /// Gets the number of soft-deleted entities.
        /// </summary>
        /// <returns>Number of soft-deleted entities.</returns>
        public static int CountSoftDeleted<TEntity>(this DbSet<TEntity> dbSet)
            where TEntity : BaseEntity, ISoftDeletable
        {
            return Guard.NotNull(dbSet)
                .IgnoreQueryFilters()
                .Count(x => x.Deleted);
        }

        /// <summary>
        /// Gets the number of soft-deleted entities.
        /// </summary>
        /// <returns>Number of soft-deleted entities.</returns>
        public static Task<int> CountSoftDeletedAsync<TEntity>(this DbSet<TEntity> dbSet, CancellationToken cancelToken = default)
            where TEntity : BaseEntity, ISoftDeletable
        {
            return Guard.NotNull(dbSet)
                .IgnoreQueryFilters()
                .CountAsync(x => x.Deleted, cancelToken);
        }

        #endregion
    }
}
