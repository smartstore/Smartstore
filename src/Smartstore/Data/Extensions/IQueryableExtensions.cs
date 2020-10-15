using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.ComponentModel;
using Smartstore.Domain;

namespace Smartstore
{
#pragma warning disable EF1001 // Internal EF Core API usage.
    public static class IQueryableExtensions
    {
        #region Static fields (EF reflection)

        private readonly static FieldInfo _queryCompilerField = typeof(EntityQueryProvider).GetField("_queryCompiler", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static FieldInfo _queryContextFactoryField = typeof(QueryCompiler).GetField("_queryContextFactory", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static FieldInfo _dependenciesField = typeof(RelationalQueryContextFactory).GetField("_dependencies", BindingFlags.NonPublic | BindingFlags.Instance);
        private readonly static PropertyInfo _stateManagerProperty = typeof(QueryContextDependencies).GetProperty("StateManager", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        #endregion

        public static TContext GetDbContext<TContext>(this IQueryable query)
            where TContext : DbContext
        {
            Guard.NotNull(query, nameof(query));

            var queryCompiler = (QueryCompiler)_queryCompilerField.GetValue(query.Provider);
            var queryContextFactory = (RelationalQueryContextFactory)_queryContextFactoryField.GetValue(queryCompiler);
            var dependencies = _dependenciesField.GetValue(queryContextFactory);
            var stateManagerObj = _stateManagerProperty.GetValue(dependencies);

            IStateManager stateManager = stateManagerObj as IStateManager ?? ((dynamic)stateManagerObj).Value;

            return (TContext)stateManager.Context;
        }

        //public static TContext GetDbContext<TEntity, TContext>(this IQueryable<TEntity> query)
        //    where TContext : DbContext
        //    where TEntity : BaseEntity
        //{
        //    var context = GetDbContext(query);

        //    if (context is not TContext)
        //    {
        //        throw new InvalidCastException($"The type of DbContext obtained from query does not match '${typeof(TContext).Name}'. Actual: ${context.GetType().Name}");
        //    }

        //    return context as TContext;
        //}

        //public static DbContext GetDbContext<TEntity>(this IQueryable<TEntity> query) 
        //    where TEntity : BaseEntity
        //{
        //    Guard.NotNull(query, nameof(query));

        //    var queryCompiler = (QueryCompiler)_queryCompilerField.GetValue(query.Provider);
        //    var queryContextFactory = (RelationalQueryContextFactory)_queryContextFactoryField.GetValue(queryCompiler);
        //    var dependencies = _dependenciesField.GetValue(queryContextFactory);
        //    var stateManagerObj = _stateManagerProperty.GetValue(dependencies);

        //    IStateManager stateManager = stateManagerObj as IStateManager ?? ((dynamic)stateManagerObj).Value;

        //    return stateManager.Context;
        //}
    }
#pragma warning restore EF1001 // Internal EF Core API usage.
}