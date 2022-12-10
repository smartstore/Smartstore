using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace Smartstore.Core.Seo.Routing;

public class ReservedSlugTable : IReservedSlugTable
{
    private readonly HashSet<string> _reservedSlugs = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _reservedPartialSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin",
        "odata", 
        "mini-profiler-resources",
        "media",
        "taskscheduler"
    };

    private readonly IActionDescriptorCollectionProvider _actionDescriptorCollectionProvider;

    public ReservedSlugTable(IActionDescriptorCollectionProvider actionDescriptorCollectionProvider)
    {
        _actionDescriptorCollectionProvider = actionDescriptorCollectionProvider;
        Initialize();
    }

    private void Initialize()
    {
        foreach (var descriptor in _actionDescriptorCollectionProvider.ActionDescriptors.Items)
        {
            var method = descriptor.ActionConstraints?.OfType<HttpMethodActionConstraint>().FirstOrDefault()?.HttpMethods.First();
            if (method is not (null or "GET"))
            {
                continue;
            }

            AnalyzeActionDescriptor(descriptor);
        }
    }

    private void AnalyzeActionDescriptor(ActionDescriptor descriptor)
    {
        if (descriptor.RouteValues["area"].EqualsNoCase("admin"))
        {
            return;
        }

        var routeInfo = descriptor.AttributeRouteInfo;
        var template = routeInfo?.Template;

        if (template.HasValue())
        {
            if (template.StartsWithNoCase("odata/"))
            {
                return;
            }

            var tokenIndex = template.IndexOf('{');
            if (tokenIndex == -1)
            {
                _reservedSlugs.Add(template);
            }
            else
            {
                _reservedPartialSlugs.Add(template[..(tokenIndex - 1)]);
            }
        }
        else
        {
            if (descriptor is ControllerActionDescriptor cad)
            {
                template = $"{cad.ControllerName}/{cad.ActionName}".ToLower();
                if (descriptor.Parameters.Count > 0) 
                {
                    _reservedPartialSlugs.Add(template);
                }
                else
                {
                    _reservedSlugs.Add(template);
                }
            }
        }
    }

    public bool IsReservedSlug(string slug) => IsReservedSlug(slug, out _);

    public bool IsReservedSlug(string slug, out string partialMatch)
    {
        Guard.NotEmpty(slug);

        partialMatch = null;
        slug = slug.Trim('/');

        if (_reservedSlugs.Contains(slug) || _reservedPartialSlugs.Contains(slug))
        {
            return true;
        }

        var slashPos = slug.LastIndexOf("/");
        while (slashPos > -1)
        {
            slug = slug[..slashPos];
            if (_reservedPartialSlugs.Contains(slug))
            {
                partialMatch = slug;
                return true;
            }

            slashPos = slug.LastIndexOf("/");
        }

        return false;
    }

    public IEnumerable<ReservedSlug> EnumerateSlugs()
    {
        return _reservedSlugs
            .Select(x => new ReservedSlug { Slug = x })
            .Concat(_reservedPartialSlugs
                .Select(x => new ReservedSlug { Slug = x, IsPrefix = true }));
    }
}
