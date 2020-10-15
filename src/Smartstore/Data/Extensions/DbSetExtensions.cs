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

        public static IEnumerable<T> GetMany<T>(this DbSet<T> dbSet, IEnumerable<int> ids) where T : BaseEntity
        {
            foreach (var chunk in ids.Slice(128))
            {
                var items = dbSet.Where(a => chunk.Contains(a.Id)).ToList();
                foreach (var item in items)
                {
                    yield return item;
                }
            }
        }

        public static async Task<IEnumerable<T>> GetManyAsync<T>(this DbSet<T> dbSet, IEnumerable<int> ids) where T : BaseEntity
        {
            var result = new List<T>();

            foreach (var chunk in ids.Slice(128))
            {
                var items = await dbSet.Where(a => chunk.Contains(a.Id)).ToListAsync();
                result.AddRange(items);
            }

            return result;
        }

        public static void Remove<T>(this DbSet<T> dbSet, int id) where T : BaseEntity, new()
        {
            Guard.NotZero(id, nameof(id));

            dbSet.Remove(new T { Id = id });
        }

        public static void RemoveRange<T>(this DbSet<T> dbSet, IEnumerable<int> ids) where T : BaseEntity, new()
        {
            Guard.NotNull(ids, nameof(ids));

            var entities = ids
                .Where(id => id > 0)
                .Select(id => new T { Id = id });

            dbSet.RemoveRange(entities);
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
        public static int DeleteAll<T>(this DbSet<T> dbSet, Expression<Func<T, bool>> predicate = null, bool cascade = false) where T : BaseEntity, new()
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
                    var entities = query.Select(x => new T { Id = x.Id }).ToList();
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

        /// <summary>
        ///     Finds an entity with the given id. If an entity with the given id
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given id
        ///     and this entity, if found, is attached to the context and returned. If no entity is found, then
        ///     null is returned.
        /// </summary>
        /// <param name="id">The primary id of the entity.</param>
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.Find(object[])"/>
        ///     because the key is known.
        /// </remarks>
        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Perf")]
        public static TEntity FindById<TEntity>(this DbSet<TEntity> dbSet, int id) 
            where TEntity : BaseEntity
        {
            return FindTracked<TEntity>(dbSet.GetDbContext(), id) ?? dbSet.FirstOrDefault(x => x.Id == id);
        }

        /// <summary>
        ///     Finds an entity with the given id. If an entity with the given id
        ///     is being tracked by the context, then it is returned immediately without making a request to the
        ///     database. Otherwise, a query is made to the database for an entity with the given id
        ///     and this entity, if found, is attached to the context and returned. If no entity is found, then
        ///     null is returned.
        /// </summary>
        /// <param name="id">The primary id of the entity.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken" /> to observe while waiting for the task to complete.</param>
        /// <returns>The entity found, or null.</returns>
        /// <remarks>
        ///     This method is slightly faster than <see cref="DbSet{TEntity}.FindAsync(object[], CancellationToken)(object[])"/>
        ///     because the key is known.
        /// </remarks>
        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Perf")]
        public static ValueTask<TEntity> FindByIdAsync<TEntity>(this DbSet<TEntity> dbSet, int id, CancellationToken cancellationToken = default) 
            where TEntity : BaseEntity
        {
            var tracked = FindTracked<TEntity>(dbSet.GetDbContext(), id);
            return tracked != null
                ? new ValueTask<TEntity>(tracked)
                : new ValueTask<TEntity>(
                    dbSet.FirstOrDefaultAsync(x => x.Id == id, cancellationToken));
        }

        [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Perf")]
        private static TEntity FindTracked<TEntity>(DbContext dbContext, int id)
            where TEntity : BaseEntity
        {
            var stateManager = dbContext.GetDependencies().StateManager;
            var key = dbContext.Model.FindEntityType(typeof(TEntity)).FindPrimaryKey();

            return stateManager.TryGetEntry(key, new object[] { id })?.Entity as TEntity;
        }
    }
}
