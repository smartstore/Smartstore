#nullable enable

using System.Diagnostics.CodeAnalysis;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Smartstore.Core.Seo.Routing
{
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
