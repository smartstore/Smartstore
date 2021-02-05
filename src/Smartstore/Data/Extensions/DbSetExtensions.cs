using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Data;
using Smartstore.Domain;
using EfState = Microsoft.EntityFrameworkCore.EntityState;

namespace Smartstore
{
    public static class DbSetExtensions
    {
        public static HookingDbContext GetDbContext<TEntity>(this DbSet<TEntity> dbSet)
            where TEntity : BaseEntity
        {
            Guard.NotNull(dbSet, nameof(dbSet));

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

            return FindTracked<TEntity>(dbSet.GetDbContext(), id) ?? dbSet.ApplyTracking(tracked).SingleOrDefault(x => x.Id == id);
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
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.Find(object[])"/>
        ///     because the key is known.
        /// </remarks>
        public static TEntity FindById<TEntity, TProperty>(this IIncludableQueryable<TEntity, TProperty> query, int id)
            where TEntity : BaseEntity
        {
            if (id == 0)
                return null;

            return FindTracked<TEntity>(query.GetDbContext(), id) ?? query.SingleOrDefault(x => x.Id == id);
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

            var trackedEntity = FindTracked<TEntity>(dbSet.GetDbContext(), id);
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
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.FindAsync(object[], CancellationToken)(object[])"/>
        ///     because the key is known.
        /// </remarks>
        public static ValueTask<TEntity> FindByIdAsync<TEntity, TProperty>(this IIncludableQueryable<TEntity, TProperty> query, int id, CancellationToken cancellationToken = default)
            where TEntity : BaseEntity
        {
            if (id == 0)
                return ValueTask.FromResult((TEntity)null);

            var trackedEntity = FindTracked<TEntity>(query.GetDbContext(), id);
            return trackedEntity != null
                ? new ValueTask<TEntity>(trackedEntity)
                : new ValueTask<TEntity>(
                    query.SingleOrDefaultAsync(x => x.Id == id, cancellationToken));
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Perf")]
        private static TEntity FindTracked<TEntity>(DbContext dbContext, int id)
            where TEntity : BaseEntity
        {
            var stateManager = dbContext.GetDependencies().StateManager;
            var key = dbContext.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey();

            return stateManager.TryGetEntry(key, new object[] { id })?.Entity as TEntity;
        }

        #endregion

        #region Get*

        /// <summary>
        /// Loads many entities from database sorted by the given id sequence.
        /// Sort is applied in-memory.
        /// </summary>
        public static IList<TEntity> GetMany<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<int> ids, bool tracked = false) 
            where TEntity : BaseEntity
        {
            Guard.NotNull(dbSet, nameof(dbSet));
            Guard.NotNull(ids, nameof(ids));

            if (!ids.Any())
                return new List<TEntity>();

            var items = dbSet
                .ApplyTracking(tracked)
                .Where(a => ids.Contains(a.Id))
                .ToList();

            return items.OrderBySequence(ids).ToList();
        }

        /// <summary>
        /// Loads many entities from database sorted by the given id sequence.
        /// Sort is applied in-memory.
        /// </summary>
        public static async Task<List<TEntity>> GetManyAsync<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<int> ids, bool tracked = false) 
            where TEntity : BaseEntity
        {
            Guard.NotNull(dbSet, nameof(dbSet));
            Guard.NotNull(ids, nameof(ids));

            if (!ids.Any())
                return new List<TEntity>();

            var items = await dbSet
                .ApplyTracking(tracked)
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();

            return items.OrderBySequence(ids).ToList();
        }

        #endregion

        #region Remove

        public static void Remove<TEntity>(this DbSet<TEntity> dbSet, int id) where TEntity : BaseEntity, new()
        {
            Guard.NotZero(id, nameof(id));

            dbSet.Remove(new TEntity { Id = id });
        }

        public static void RemoveRange<TEntity>(this DbSet<TEntity> dbSet, IEnumerable<int> ids) where TEntity : BaseEntity, new()
        {
            Guard.NotNull(ids, nameof(ids));

            var entities = ids
                .Where(id => id > 0)
                .Select(id => new TEntity { Id = id });

            dbSet.RemoveRange(entities);
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

            using (var scope = new DbContextScope(ctx: ctx, autoDetectChanges: false, hooksEnabled: false))
            {
                var query = dbSet.AsQueryable();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                if (cascade)
                {
                    var records = query.ToList();
                    foreach (var chunk in records.Slice(500))
                    {
                        dbSet.RemoveRange(chunk.ToList());
                        numDeleted += ctx.SaveChanges();
                    }
                }
                else
                {
                    var entities = query.Select(x => new TEntity { Id = x.Id }).ToList();
                    foreach (var chunk in entities.Slice(500))
                    {
                        dbSet.RemoveRange(chunk);
                        numDeleted += ctx.SaveChanges();
                    }
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

            using (var scope = new DbContextScope(ctx: ctx, autoDetectChanges: false, hooksEnabled: false))
            {
                var query = dbSet.AsQueryable();
                if (predicate != null)
                {
                    query = query.Where(predicate);
                }

                if (cascade)
                {
                    var records = await query.ToListAsync();
                    foreach (var chunk in records.Slice(500))
                    {
                        dbSet.RemoveRange(chunk.ToList());
                        numDeleted += await ctx.SaveChangesAsync();
                    }
                }
                else
                {
                    var entities = await query.Select(x => new T { Id = x.Id }).ToListAsync();
                    foreach (var chunk in entities.Slice(500))
                    {
                        dbSet.RemoveRange(chunk);
                        numDeleted += await ctx.SaveChangesAsync();
                    }
                }
            }

            return numDeleted;
        }

        #endregion
    }
}
