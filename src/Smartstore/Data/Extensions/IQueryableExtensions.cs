using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.ComponentModel;
using Smartstore.Data;
using Smartstore.Domain;

namespace Smartstore
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.", Justification = "Pending")]
    public static class IQueryableExtensions
    {
        public readonly static MethodInfo StringSubstringMethod = typeof(string)
            .GetMethod(nameof(string.Substring), [typeof(int), typeof(int)]);

        private readonly static ConcurrentDictionary<Type, LambdaExpression> _memberInitExpressions = new();
        
        #region EF reflection

        private readonly static FieldInfo _queryCompilerField = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static FieldInfo _queryContextFactoryField = typeof(QueryCompiler).GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static PropertyInfo _dependenciesProperty = typeof(RelationalQueryContextFactory).GetProperty("Dependencies", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static PropertyInfo _stateManagerProperty = typeof(QueryContextDependencies).GetProperty("StateManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        #endregion

        /// <summary>
        /// Applies "AsTracking()" or "AsNoTracking()" according to <paramref name="tracked"/> parameter.
        /// </summary>
        public static IQueryable<T> ApplyTracking<T>(this IQueryable<T> query, bool tracked)
            where T : BaseEntity
        {
            Guard.NotNull(query);

            return tracked ? query.AsTracking() : query.AsNoTracking();
        }

        /// <summary>
        /// Applies a filter that selects entities by matching identifiers.
        /// </summary>
        /// <param name="ids">Sequence of ids to match</param>
        public static IQueryable<T> ApplyIdFilter<T>(this IQueryable<T> query, IEnumerable<int> ids)
            where T : BaseEntity
        {
            Guard.NotNull(query);
            Guard.NotNull(ids);

            if (ids.TryGetNonEnumeratedCount(out var count) && count < 2)
            {
                return count == 0 ? query : query.Where(x => x.Id == ids.ElementAt(0));
            }

            return query.Where(x => ids.Contains(x.Id));
        }

        /// <summary>
        /// Creates and caches a <c>Select</c> lambda expression for entity type <typeparamref name="T"/> 
        /// that maps all intrinsic properties, but excluding properties annotated with
        /// <see cref="NonSummaryAttribute"/> attribute.
        /// </summary>
        /// <remarks>
        /// Lazy loading and change tracking will be disabled for summary lists.
        /// </remarks>
        public static IQueryable<T> SelectSummary<T>(this IQueryable<T> query)
            where T : BaseEntity
        {
            Guard.NotNull(query);

            var selector = GetEntitySummarySelector<T>(query.GetDbContext().Model);

            if (selector != null)
            {
                return query.Select(selector);
            }
            else
            {
                return query;
            }
        }

        private static Expression<Func<T, T>> GetEntitySummarySelector<T>(IModel entityModel)
            where T : BaseEntity
        {
            // x => ...
            var local = Expression.Parameter(typeof(T), "x");

            var selectExpression = _memberInitExpressions.GetOrAdd(typeof(T), key =>
            {
                // new T { }
                var newEntity = Expression.New(typeof(T));

                var memberBindings = new List<MemberBinding>();
                var numNonSummaryAttributes = 0;

                var entityType = entityModel.FindEntityType(typeof(T));
                var props = FastProperty.GetProperties(typeof(T));

                foreach (var kvp in props)
                {
                    var prop = kvp.Value.Property;

                    if (!kvp.Value.IsPublicSettable)
                    {
                        continue;
                    }

                    var nonSummaryAttribute = prop.GetAttribute<NonSummaryAttribute>(true);
                    if (nonSummaryAttribute != null)
                    {
                        numNonSummaryAttributes++;

                        if (nonSummaryAttribute.MaxLength == null || prop.PropertyType != typeof(string))
                        {
                            // A specified MaxLength has no effect on non-string properties.
                            continue;
                        }
                    }

                    if (prop.HasAttribute<NotMappedAttribute>(true))
                    {
                        continue;
                    }

                    if (!prop.PropertyType.IsBasicOrNullableType())
                    {
                        // Always select a complex property that has a value converter.
                        if (entityType.FindProperty(prop)?.GetValueConverter() is null)
                        {
                            continue;
                        }
                    }

                    // { Name = --> x.Name <-- }
                    Expression sourceExpression = Expression.Property(local, prop);

                    if (nonSummaryAttribute?.MaxLength > 0)
                    {
                        // { Name = x.Name-->.Substring(0, MaxLength)<-- }
                        sourceExpression = Expression.Call(
                            // x.Name
                            sourceExpression, 
                            // it.Substring()
                            StringSubstringMethod,
                            // it.Substring(--> 0 <--, ...)
                            Expression.Constant(0),
                            // it.Substring(0, --> MaxLength <--)
                            Expression.Constant(nonSummaryAttribute.MaxLength.Value));
                    }

                    // { --> Name = x.Name <-- }
                    var binding = Expression.Bind(prop, sourceExpression);
                    memberBindings.Add(binding);
                }

                if (numNonSummaryAttributes == 0)
                {
                    // If the scanned entity is not annotated with NonSummaryAttribute,
                    // we should cache null. We won't call .Select(...) in that case later.
                    return null;
                }

                // Create a MemberInitExpression that represents initializing
                // all summary members of the 'T' class.
                // new T { Id = x.Id, Name = x.Name, ... }
                var entityInitExpression = Expression.MemberInit(newEntity, memberBindings);

                // Convert to lamdba
                return Expression.Lambda<Func<T, T>>(entityInitExpression, local);
            });

            return (Expression<Func<T, T>>)selectExpression;
        }

        /// <summary>
        /// FastPager ensures stable and consistent paging performance over very large datasets.
        /// Other than LINQs Skip(x).Take(y) approach the entity set is sorted 
        /// descending by id and a specified amount of records are returned.
        /// The FastPager remembers the last (lowest) returned id and uses
        /// it for the next batches' WHERE clause. This way Skip() can be avoided which
        /// is known for performing really bad on large tables.
        /// </summary>
        public static FastPager<T> ToFastPager<T>(this IQueryable<T> query, int pageSize = 1000)
            where T : BaseEntity
        {
            Guard.NotNull(query);

            return new FastPager<T>(query, pageSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TContext GetDbContext<TContext>(this IQueryable query) where TContext : HookingDbContext
        {
            return (TContext)GetDbContext(query);
        }

        public static HookingDbContext GetDbContext(this IQueryable query)
        {
            Guard.NotNull(query);

            var queryCompiler = (QueryCompiler)_queryCompilerField.GetValue(query.Provider);
            var queryContextFactory = _queryContextFactoryField.GetValue(queryCompiler);

            // In unit tests we have to deal with "InMemoryQueryContextFactory"
            var dependencies = queryContextFactory is RelationalQueryContextFactory relationalFactory
                ? _dependenciesProperty.GetValue(queryContextFactory)
                : queryContextFactory.GetType()
                    .GetProperty("Dependencies", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .GetValue(queryContextFactory);

            var stateManagerObj = _stateManagerProperty.GetValue(dependencies);

            IStateManager stateManager = stateManagerObj as IStateManager ?? ((dynamic)stateManagerObj).Value;

            return (HookingDbContext)stateManager.Context;
        }

        /// <summary>
        /// Loads many entities from database sorted by the given id sequence.
        /// Sort is applied in-memory.
        /// </summary>
        public static IList<TEntity> GetMany<TEntity>(this IQueryable<TEntity> query, IEnumerable<int> ids, bool tracked = false)
            where TEntity : BaseEntity
        {
            Guard.NotNull(query);
            Guard.NotNull(ids);

            if (!ids.Any())
            {
                return new List<TEntity>();
            }

            var items = query
                .ApplyTracking(tracked)
                .Where(a => ids.Contains(a.Id))
                .ToList();

            return items.OrderBySequence(ids).ToList();
        }

        /// <summary>
        /// Loads many entities from database sorted by the given id sequence.
        /// Sort is applied in-memory.
        /// </summary>
        public static async Task<List<TEntity>> GetManyAsync<TEntity>(this IQueryable<TEntity> query, IEnumerable<int> ids, bool tracked = false)
            where TEntity : BaseEntity
        {
            Guard.NotNull(query);
            Guard.NotNull(ids);

            if (!ids.Any())
            {
                return new List<TEntity>();
            }

            var items = await query
                .ApplyTracking(tracked)
                .Where(a => ids.Contains(a.Id))
                .ToListAsync();

            return items.OrderBySequence(ids).ToList();
        }

        #region ExecuteDelete

        /// <inheritdoc cref="RelationalQueryableExtensions.ExecuteDelete{TSource}(IQueryable{TSource})"/>
        /// <summary>
        /// Deletes database rows for the entity instances which match the LINQ query from the database in bulk.
        /// </summary>
        /// <param name="bulkSize">Number of rows to be deleted in a single delete operation.</param>
        public static int ExecuteDelete<TEntity>(this IQueryable<TEntity> query, int bulkSize)
            where TEntity : BaseEntity
        {
            Guard.NotNull(query);
            Guard.IsPositive(bulkSize);

            var numTotal = 0;

            query = query.OrderBy(x => x.Id).Take(bulkSize);

            while (true)
            {
                var num = query.ExecuteDelete();
                numTotal += num;

                if (num < bulkSize)
                {
                    break;
                }
            }

            return numTotal;
        }

        /// <inheritdoc cref="RelationalQueryableExtensions.ExecuteDeleteAsync{TSource}(IQueryable{TSource}, CancellationToken)"/>
        /// <summary>
        /// Asynchronously deletes database rows for the entity instances which match the LINQ query from the database in bulk.
        /// </summary>
        /// <param name="bulkSize">Number of rows to be deleted in a single delete operation.</param>
        public static async Task<int> ExecuteDeleteAsync<TEntity>(this IQueryable<TEntity> query, int bulkSize, CancellationToken cancellationToken = default)
            where TEntity : BaseEntity
        {
            Guard.NotNull(query);
            Guard.IsPositive(bulkSize);

            var numTotal = 0;

            query = query.OrderBy(x => x.Id).Take(bulkSize);

            while (true)
            {
                var num = await query.ExecuteDeleteAsync(cancellationToken);
                numTotal += num;

                if (num < bulkSize)
                {
                    break;
                }
            }

            return numTotal;
        }

        #endregion
    }
}