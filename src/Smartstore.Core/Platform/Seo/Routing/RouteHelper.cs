#nullable enable

using System.Diagnostics.CodeAnalysis;
using Autofac;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;

namespace Smartstore.Core.Seo.Routing
{
    public class RouteHelper : IRouteHelper
    {
        #region Static

        private static RouteOptions? _routeOptions;

        /// <summary>
        /// For testing purposes.
        /// </summary>
        internal static RouteOptions RouteOptions
        {
            get
            {
                _routeOptions ??= EngineContext.Current.Application.Services.ResolveOptional<IOptions<RouteOptions>>()?.Value;
                return _routeOptions ?? new RouteOptions { AppendTrailingSlash = true, LowercaseUrls = true };
            }
            set => _routeOptions = Guard.NotNull(value);
        }

        /// <summary>
        /// Normalizes a path component according to <c>RouteOptions.AppendTrailingSlash</c>
        /// and <c>RouteOptions.LowercaseUrls</c>. Call this method if you didn't obtain
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
        /// Normalizes a query string component according to <c>RouteOptions.LowercaseQueryStrings</c>. 
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
        /// Normalizes a query string component according to <c>RouteOptions.LowercaseQueryStrings</c>. 
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

        #endregion

        private readonly HashSet<string> _disallowRobotPaths = new();
        private readonly HashSet<string> _reservedPaths = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _reservedPartialPaths = new(StringComparer.OrdinalIgnoreCase)
        {
            "admin",
            "odata",
            "mini-profiler-resources",
            "media",
            "taskscheduler"
        };

        private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;
        private readonly TrailingSlashRule _trailingSlashRule;

        public RouteHelper(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider, SeoSettings seoSettings)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            _trailingSlashRule = seoSettings.TrailingSlashRule;

            Initialize();
        }

        private void Initialize()
        {
            foreach (var descriptor in _actionDescriptorCollectionProvider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>())
            {
                var path = AnalyzeReservedSlug(descriptor);
                if (!string.IsNullOrEmpty(path))
                {
                    AnalyzeDisallowRobot(descriptor, path);
                }
            }
        }

        private string? AnalyzeReservedSlug(ControllerActionDescriptor descriptor)
        {
            var method = descriptor.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods.First();
            if (method is not (null or "GET"))
            {
                return null;
            }

            if (descriptor.RouteValues["area"].EqualsNoCase("admin"))
            {
                return null;
            }

            var routeInfo = descriptor.AttributeRouteInfo;
            var template = routeInfo?.Template;
            string result;

            if (!string.IsNullOrEmpty(template))
            {
                if (template.StartsWithNoCase("odata/"))
                {
                    return null;
                }

                var tokenIndex = template.IndexOf('{');
                if (tokenIndex == -1)
                {
                    result = template;
                    _reservedPaths.Add(result);
                }
                else
                {
                    result = template[..(tokenIndex - 1)];
                    _reservedPartialPaths.Add(result);
                }
            }
            else
            {
                template = NormalizePath($"{descriptor.ControllerName}/{descriptor.ActionName}");
                result = template;
                if (descriptor.Parameters.Count > 0)
                {
                    _reservedPartialPaths.Add(result);
                }
                else
                {
                    _reservedPaths.Add(result);
                }
            }

            return result;
        }

        private void AnalyzeDisallowRobot(ControllerActionDescriptor descriptor, string path)
        {
            var disallowAttribute = descriptor.FilterDescriptors
                .Where(x => x.Scope == FilterScope.Action || x.Scope == FilterScope.Controller)
                .Select(x => x.Filter)
                .OfType<DisallowRobotAttribute>()
                .FirstOrDefault();

            if (disallowAttribute != null)
            {
                if (disallowAttribute.ExactPath)
                {
                    path = path.TrimEnd('/').EnsureStartsWith('/');
                    // "/cart$"
                    _disallowRobotPaths.Add(path + '$');
                    // "/cart/$"
                    _disallowRobotPaths.Add(path.EnsureEndsWith('/') + '$');
                }
                else
                {
                    // "/product/clearcomparelist"
                    _disallowRobotPaths.Add(path.EnsureStartsWith('/'));
                }
            }
        }

        private string NormalizePath(string path)
        {
            if (_trailingSlashRule == TrailingSlashRule.Allow)
            {
                return path;
            }

            return NormalizePathComponent(path);
        }

        public bool IsReservedPath(string slug) => IsReservedPath(slug, out _);

        public bool IsReservedPath(string slug, out string? partialMatch)
        {
            Guard.NotEmpty(slug);

            partialMatch = null;
            slug = slug.Trim('/');

            if (_reservedPaths.Contains(slug) || _reservedPartialPaths.Contains(slug))
            {
                return true;
            }

            var slashPos = slug.LastIndexOf("/");
            while (slashPos > -1)
            {
                slug = slug[..slashPos];
                if (_reservedPartialPaths.Contains(slug))
                {
                    partialMatch = slug;
                    return true;
                }

                slashPos = slug.LastIndexOf("/");
            }

            return false;
        }

        public IEnumerable<ReservedPath> EnumerateReservedPaths()
        {
            return _reservedPaths
                .Select(x => new ReservedPath(x, false))
                .Concat(_reservedPartialPaths
                    .Select(x => new ReservedPath(x, true)));
        }

        public IEnumerable<string> EnumerateDisallowedRobotPaths()
        {
            return _disallowRobotPaths;
        }
    }
}