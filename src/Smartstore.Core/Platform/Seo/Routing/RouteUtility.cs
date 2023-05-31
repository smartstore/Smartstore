#nullable enable

using System.Diagnostics.CodeAnalysis;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Smartstore.Core.Seo.Routing
{
    /// <summary>
    /// Helper class for link normalization.
    /// </summary>
    public static class RouteUtility
    {
        private static RouteOptions _routeOptions = default!;

        /// <summary>
        /// For testing purposes.
        /// </summary>
        internal static RouteOptions RouteOptions
        {
            get => _routeOptions ??= (EngineContext.Current.Application.Services.ResolveOptional<IOptions<RouteOptions>>()?.Value ?? new RouteOptions());
            set => _routeOptions = Guard.NotNull(value);
        }

        /// <summary>
        /// Normalizes a path component according to <see cref="RouteOptions.AppendTrailingSlash"/>
        /// and <see cref="RouteOptions.LowercaseUrls"/>. Call this method if you didn't obtain
        /// <paramref name="path"/> from any <see cref="IUrlHelper"/> or <see cref="LinkGenerator"/>
        /// method.
        /// </summary>
        /// <param name="path">The path to normalize.</param>
        /// <returns>The normalized path.</returns>
        [return: NotNullIfNotNull(nameof(path))]
        public static string? NormalizePathComponent(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            if (RouteOptions.AppendTrailingSlash)
            {
                if (path[^1] != '/')
                {
                    path += '/';
                }
            }
            else
            {
                if (path[^1] == '/')
                {
                    path = path[..^1];
                }
            }

            if (RouteOptions.LowercaseUrls && path.Any(char.IsUpper))
            {
                path = path.ToLowerInvariant();
            }

            return path;
        }

        /// <summary>
        /// Normalizes a query string component according to <see cref="RouteOptions.LowercaseQueryStrings"/>. 
        /// Call this method if you didn't obtain <paramref name="queryString"/> 
        /// from any <see cref="IUrlHelper"/> or <see cref="LinkGenerator"/> method.
        /// </summary>
        /// <param name="queryString">The query string to normalize.</param>
        /// <returns>The normalized query string.</returns>
        [return: NotNullIfNotNull(nameof(queryString))]
        public static string? NormalizeQueryComponent(string? queryString)
        {
            if (string.IsNullOrEmpty(queryString))
            {
                return queryString;
            }

            if (RouteOptions.LowercaseUrls && RouteOptions.LowercaseQueryStrings && queryString.Any(char.IsUpper))
            {
                queryString = queryString.ToLowerInvariant();
            }

            return queryString;
        }

        /// <summary>
        /// Normalizes a query string component according to <see cref="RouteOptions.LowercaseQueryStrings"/>. 
        /// </summary>
        /// <param name="queryString">The query string to normalize.</param>
        /// <returns>The normalized query string.</returns>
        [return: NotNullIfNotNull(nameof(queryString))]
        public static QueryString? NormalizeQueryComponent(QueryString? queryString)
        {
            if (queryString == null)
            {
                return null;
            }

            var str = queryString.Value.Value;
            if (string.IsNullOrEmpty(str))
            {
                return queryString;
            }

            if (RouteOptions.LowercaseUrls && RouteOptions.LowercaseQueryStrings && str.Any(char.IsUpper))
            {
                str = str.ToLowerInvariant();
            }

            return new QueryString(str);
        }
    }
}
