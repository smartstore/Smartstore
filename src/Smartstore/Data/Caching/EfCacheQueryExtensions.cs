using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Smartstore.Domain;

namespace Smartstore.Data.Caching
{
    public static class EfCachedQueryExtensions
    {
        private static readonly TimeSpan _defaultTimeout = TimeSpan.FromHours(1);

        private static readonly MethodInfo _asNoTrackingMethodInfo =
            typeof(EntityFrameworkQueryableExtensions).GetTypeInfo().GetDeclaredMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking));

        /// <summary>
        /// IsNotCachable Marker
        /// </summary>
        public static readonly string IsNotCachableMarker = $"{nameof(EfCacheInterceptor)}{nameof(NotCacheable)}";

        /// <summary>
        /// Returns a new query where the entities returned will be cached for 1 hour.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The input query.</param>
        public static IQueryable<T> Cacheable<T>(this IQueryable<T> query)
            where T : BaseEntity
        {
            return Cacheable(query, _defaultTimeout);
        }

        /// <summary>
        /// Returns a new query where the entities returned will be cached.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The input query.</param>
        /// <param name="duration">The expiration timeout.</param>
        /// <param name="cacheDependencies">
        /// Set this option to the `real` related table names of the current query, if you are using an stored procedure,
        /// otherswise cache dependencies of normal queries will be calculated automatically.
        /// `cacheDependencies` determines which tables are used in this final query.
        /// This array will be used to invalidate the related cache of all related queries automatically.
        /// </param>
        public static IQueryable<T> Cacheable<T>(this IQueryable<T> query, TimeSpan duration, params string[] cacheDependencies)
            where T : BaseEntity
        {
            SanityCheck(query);

            return query.MarkAsNoTracking().TagWith(EfCachePolicy.Configure(options =>
            {
                options.Timeout(duration);

                if (cacheDependencies.Length > 0)
                {
                    options.CacheDependencies(cacheDependencies);
                }
            }));
        }

        /// <summary>
        /// Returns a new query where the entities returned will not be cached.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The input query.</param>
        public static IQueryable<T> NotCacheable<T>(this IQueryable<T> query)
            where T : BaseEntity
        {
            SanityCheck(query);
            return query.TagWith(IsNotCachableMarker);
        }

        /// <summary>
        /// Returns a new query where the entities returned will not be cached.
        /// </summary>
        /// <typeparam name="T">Entity type.</typeparam>
        /// <param name="query">The input query.</param>
        public static IQueryable<T> NotCacheable<T>(this DbSet<T> query)
            where T : BaseEntity
        {
            SanityCheck(query);
            return query.TagWith(IsNotCachableMarker);
        }

        private static void SanityCheck(IQueryable query)
        {
            if (query.Provider is not EntityQueryProvider)
            {
                throw new NotSupportedException("`Cacheable` method is designed only for relational EF Core queries.");
            }
        }

        private static IQueryable<T> MarkAsNoTracking<T>(this IQueryable<T> query)
            where T : BaseEntity
        {
            if (typeof(T).GetTypeInfo().IsClass)
            {
                return query.Provider.CreateQuery<T>(
                    Expression.Call(null, _asNoTrackingMethodInfo.MakeGenericMethod(typeof(T)), query.Expression));
            }

            return query;
        }
    }
}
