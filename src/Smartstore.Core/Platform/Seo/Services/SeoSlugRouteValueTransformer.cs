using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Core.Data;

namespace Smartstore.Core.Seo
{
    public class SeoSlugRouteValueTransformer : DynamicRouteValueTransformer
    {
        private readonly SmartDbContext _db;

        public SeoSlugRouteValueTransformer(SmartDbContext db)
        {
            _db = db;
        }

        public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            // TODO: (core) Implement SeoSlugRouteValueTransformer.

            var slug = (string)values["slug"];

            if (slug.IsEmpty())
            {
                return null;
            }

            var urlRecord = await _db.UrlRecords
                .AsNoTracking()
                .ApplySlugFilter(slug, true)
                .FirstOrDefaultAsync();

            if (urlRecord == null)
            {
                return null;
            }

            if (!urlRecord.IsActive)
            {
                var activeSlug = string.Empty;
                if (activeSlug.HasValue())
                {
                    // TODO: (core) Redirect? Here? How?
                }
            }

            return null;

            //return new RouteValueDictionary
            //{
            //    { "area", "" },
            //    { "controller", "controllerName" },
            //    { "action", "actionName" },
            //    { "id", urlRecord.EntityId }
            //};
        }
    }
}
