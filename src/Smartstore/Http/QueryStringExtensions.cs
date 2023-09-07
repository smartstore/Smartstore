#nullable enable

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Smartstore.Collections;

namespace Smartstore
{
    public static class QueryStringExtensions
    {
        #region Merge

        /// <inheritdoc cref="Merge(QueryString, QueryString)" />
        /// <param name="other">The QueryString to merge into the source QueryString, with or without leading '?'.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryString Merge(this QueryString source, string? other)
        {
            if (!string.IsNullOrEmpty(other) && other[0] != '?')
            {
                return Merge(source, new QueryString('?' + other));
            }
            
            return Merge(source, new QueryString(other));
        }

        /// <summary>
        /// Merges two QueryString objects into a single QueryString, combining their entries.
        /// </summary>
        /// <param name="source">The source QueryString to merge with.</param>
        /// <param name="other">The QueryString to merge into the source QueryString.</param>
        /// <returns>
        /// A new QueryString containing entries from both the source and other QueryStrings.
        /// If both source and other are empty or null, an empty QueryString is returned.
        /// If either source or other is empty or null, the non-empty QueryString is returned.
        /// </returns>
        /// <remarks>
        /// If both source and other QueryStrings have values, the entries from the other QueryString
        /// are added to the source QueryString. If a key already exists in the source QueryString,
        /// its value is overwritten with the value from the other QueryString.
        /// </remarks>
        public static QueryString Merge(this QueryString source, QueryString other)
        {
            if (!source.HasValue)
            {
                return other;
            }

            if (!other.HasValue)
            {
                return source;
            }

            return MergeQueryStringInternal(
                new MutableQueryCollection(source.Value),
                other);
        }

        /// <inheritdoc cref="Merge(QueryString, QueryString)" />
        /// <param name="source">The source query collection to merge with.</param>
        /// <param name="other">The QueryString to merge into the source QueryString, with or without leading '?'.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueryString Merge(this IQueryCollection? source, string? other)
        {
            if (!string.IsNullOrEmpty(other) && other[0] != '?')
            {
                return Merge(source, new QueryString('?' + other));
            }

            return Merge(source, new QueryString(other));
        }

        /// <inheritdoc cref="Merge(QueryString, QueryString)" />
        /// <param name="source">The source query collection to merge with.</param>
        public static QueryString Merge(this IQueryCollection? source, QueryString other)
        {
            if (source == null || source.Count == 0)
            {
                return other;
            }

            if (!other.HasValue)
            {
                return QueryString.Empty;
            }

            return MergeQueryStringInternal(
                new MutableQueryCollection(new Dictionary<string, StringValues>(source)), 
                other);
        }

        private static QueryString MergeQueryStringInternal(MutableQueryCollection source, QueryString other)
        {
            var modify = QueryHelpers.ParseQuery(other.Value);

            foreach (var kvp in modify)
            {
                source.Add(kvp.Key, kvp.Value, true);
            }

            return source.ToQueryString();
        }

        #endregion

        #region Remove

        /// <summary>
        /// Removes a parameter from the <paramref name="source"/> query string if it exists.
        /// </summary>
        /// <param name="source">The source QueryString to remove the parameter from.</param>
        /// <param name="paramName">The parameter to remove</param>
        /// <returns>
        /// A new QueryString containing all entries from <paramref name="source"/>
        /// except <paramref name="paramName"/>.
        /// </returns>
        public static QueryString RemoveParam(this QueryString source, string? paramName)
        {
            if (!source.HasValue)
            {
                return QueryString.Empty;
            }

            if (string.IsNullOrEmpty(paramName))
            {
                return source;
            }

            var current = new MutableQueryCollection(source.Value);

            if (!current.ContainsKey(paramName))
            {
                return source;
            }

            current.Remove(paramName);

            return current.ToQueryString();
        }

        /// <summary>
        /// Removes a parameter from the <paramref name="source"/> query string if it exists.
        /// </summary>
        /// <param name="source">The source QueryString to remove the parameter from.</param>
        /// <param name="paramName">The parameter to remove</param>
        /// <returns>
        /// A new QueryString containing all entries from <paramref name="source"/>
        /// except <paramref name="paramName"/>.
        /// </returns>
        public static QueryString RemoveParam(this IQueryCollection source, string? paramName)
        {
            if (source == null || source.Count == 0)
            {
                return QueryString.Empty;
            }

            if (string.IsNullOrEmpty(paramName))
            {
                return new QueryBuilder(source).ToQueryString();
            }

            var current = new MutableQueryCollection(new Dictionary<string, StringValues>(source));

            current.Remove(paramName);

            return current.ToQueryString();
        }

        #endregion
    }
}
