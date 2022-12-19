using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Smartstore.Core.Seo.Routing
{
    public class DefaultRouteHelper : IRouteHelper
    {
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

        public DefaultRouteHelper(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
        {
            _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
            Initialize();
        }

        private void Initialize()
        {
            foreach (var descriptor in _actionDescriptorCollectionProvider.ActionDescriptors.Items.OfType<ControllerActionDescriptor>())
            {
                var path = AnalyzeReservedSlug(descriptor);
                if (path.HasValue())
                {
                    AnalyzeDisallowRobot(descriptor, path);
                }
            }
        }

        private string AnalyzeReservedSlug(ControllerActionDescriptor descriptor)
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

            if (template.HasValue())
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
                template = $"{descriptor.ControllerName}/{descriptor.ActionName}".ToLower();
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
                    _disallowRobotPaths.Add(path.TrimEnd('/').EnsureStartsWith('/'));
                }
            }
        }

        public bool IsReservedPath(string slug) => IsReservedPath(slug, out _);

        public bool IsReservedPath(string slug, out string partialMatch)
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