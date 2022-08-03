using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.Data;
using Smartstore.Domain;

namespace Smartstore
{
#pragma warning disable EF1001 // Internal EF Core API usage.
    public static class IQueryableExtensions
    {
        #region Static fields (EF reflection)

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
            Guard.NotNull(query, nameof(query));

            return tracked ? query.AsTracking() : query.AsNoTracking();
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
            Guard.NotNull(query, nameof(query));

            return new FastPager<T>(query, pageSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TContext GetDbContext<TContext>(this IQueryable query) where TContext : HookingDbContext
        {
            return (TContext)GetDbContext(query);
        }

        public static HookingDbContext GetDbContext(this IQueryable query)
        {
            Guard.NotNull(query, nameof(query));

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
    }
#pragma warning restore EF1001 // Internal EF Core API usage.
}