using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Smartstore.Domain;

namespace Smartstore.Data.Caching
{
    public static class CachingQueryExtensions
    {
        internal static readonly MethodInfo AsNoTrackingMethodInfo =
            typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo()
            .GetDeclaredMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTracking));

        internal static readonly MethodInfo AsNoTrackingWithIdentityResolutionMethodInfo =
            typeof(EntityFrameworkQueryableExtensions)
            .GetTypeInfo()
            .GetDeclaredMethod(nameof(EntityFrameworkQueryableExtensions.AsNoTrackingWithIdentityResolution));

        internal static readonly MethodInfo AsCachingMethodInfo =
            typeof(CachingQueryExtensions)
            .GetTypeInfo()
            .GetMethods()
            .Where(m => m.Name == nameof(AsCaching))
            .Where(m => m.GetParameters().Any(p => p.ParameterType == typeof(DbCachingPolicy)))
            .Single();

        /// <summary>
        /// <para>
        ///     The second level cache will not cache any entities that are returned from a LINQ query,
        ///     nor will it return any previously cached result.
        /// </para>
        /// <para>
        ///     Call this method when you want to suppress a global caching policy that has been assigned to an entity type.
        /// </para>
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <returns>A new query where the result set will not be cached.</returns>
        public static IQueryable<T> AsNoCaching<T>(this IQueryable<T> source)
            where T : BaseEntity
        {
            Guard.NotNull(source, nameof(source));

            return source.AsCaching<T>(new DbCachingPolicy { NoCaching = true });
        }

        /// <summary>
        /// Returns a new query where the result will be cached.
        /// Only untracked entities will be cached.
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <returns>A new query where the result set will be cached.</returns>
        public static IQueryable<T> AsCaching<T>(this IQueryable<T> source)
            where T : BaseEntity
        {
            Guard.NotNull(source, nameof(source));

            return source.AsCaching<T>(new DbCachingPolicy());
        }

        /// <summary>
        /// Returns a new query where the result will be cached base on the <see cref="duration"/> parameter.
        /// Only untracked entities will be cached.
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="duration">Limits the lifetime of cached query results.</param>
        /// <returns>A new query where the result set will be cached.</returns>
        public static IQueryable<T> AsCaching<T>(this IQueryable<T> source, [NotParameterized] TimeSpan duration)
            where T : BaseEntity
        {
            Guard.NotNull(source, nameof(source));

            if (duration < TimeSpan.Zero)
            {
                throw new ArgumentException($"Invalid caching timeout {duration}", nameof(duration));
            }

            return source.AsCaching<T>(new DbCachingPolicy { ExpirationTimeout = duration });
        }

        /// <summary>
        /// Returns a new query where the result will be cached if its item count does not exceed given <paramref name="maxRows"/>.
        /// Only untracked entities will be cached.
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="maxRows">Query results with more items than the given number will not be cached..</param>
        /// <returns>A new query where the result set will be cached.</returns>
        public static IQueryable<T> AsCaching<T>(this IQueryable<T> source, [NotParameterized] int maxRows)
            where T : BaseEntity
        {
            Guard.NotNull(source, nameof(source));
            Guard.IsPositive(maxRows, nameof(maxRows));

            return source.AsCaching<T>(new DbCachingPolicy { MaxRows = maxRows });
        }

        /// <summary>
        /// Returns a new query where the result will be cached base on the <see cref="duration"/> parameter.
        /// Only untracked entities will be cached.
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="duration">Limits the lifetime of cached query results.</param>
        /// <param name="maxRows">Query results with more items than the given number will not be cached..</param>
        /// <returns>A new query where the result set will be cached.</returns>
        public static IQueryable<T> AsCaching<T>(this IQueryable<T> source, [NotParameterized] TimeSpan duration, [NotParameterized] int maxRows)
            where T : BaseEntity
        {
            Guard.NotNull(source, nameof(source));
            Guard.IsPositive(maxRows, nameof(maxRows));

            if (duration < TimeSpan.Zero)
            {
                throw new ArgumentException($"Invalid caching timeout {duration}", nameof(duration));
            }

            return source.AsCaching<T>(new DbCachingPolicy { ExpirationTimeout = duration, MaxRows = maxRows });
        }

        /// <summary>
        /// Returns a new query where the result will be cached.
        /// Only untracked entities will be cached.
        /// </summary>
        /// <typeparam name="T">The type of entity being queried.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="options">Options how to handle cached query results.</param>
        /// <returns>A new query where the result set will be cached.</returns>
        public static IQueryable<T> AsCaching<T>(this IQueryable<T> source, [NotParameterized] DbCachingPolicy policy)
            where T : BaseEntity
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(policy, nameof(policy));

            if (source.Provider is not CachingQueryProvider)
            {
                // The AsCaching() method expression will result in a LINQ translation error
                // if ef caching is not active.
                return source;
            }

            return
                source.Provider.CreateQuery<T>(
                    Expression.Call(
                        instance: null,
                        method: AsCachingMethodInfo.MakeGenericMethod(typeof(T)),
                        arg0: source.Expression,
                        arg1: Expression.Constant(policy)));
        }
    }
}
