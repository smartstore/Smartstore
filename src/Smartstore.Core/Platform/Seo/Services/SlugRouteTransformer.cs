using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Smartstore.Collections;
using Smartstore.Core.Data;
using Smartstore.Core.Localization;

namespace Smartstore.Core.Seo
{
    public class SlugRouteTransformer : DynamicRouteValueTransformer
    {
        private readonly SmartDbContext _db;
        private readonly IUrlService _urlService;
        private readonly LocalizationSettings _localizationSettings;

        public SlugRouteTransformer(SmartDbContext db, IUrlService urlService, LocalizationSettings localizationSettings)
        {
            _db = db;
            _urlService = urlService;
            _localizationSettings = localizationSettings;
        }

        #region Static

        public const string SlugRouteKey = "SeName";

        // Key = Prefix, Value = EntityType
        private static readonly Multimap<string, string> _urlPrefixes = new Multimap<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly List<SlugRouter> _routers = new();

        static SlugRouteTransformer()
        {
            _routers.Add(new DefaultSlugRouter());
        }

        /// <summary>
        /// Gets all registered slug routers as ordered readonly sequence.
        /// </summary>
        public static IEnumerable<SlugRouter> Routers { get; } = _routers.OrderBy(x => x.Order);

        /// <summary>
        /// Registers a router that can generate route values for a matched <see cref="UrlRecord"/> entity.
        /// </summary>
        /// <param name="router">The router to register.</param>
        public static void RegisterRouter(SlugRouter router)
        {
            Guard.NotNull(router, nameof(router));
            _routers.Add(router);
        }

        /// <summary>
        /// Registers a url prefix for a range of entity names. 
        /// E.g. the prefix "shop" for entity name "product" would result in
        /// product url "shop/any-product-slug".
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="entityNames"></param>
        public static void RegisterUrlPrefix(string prefix, params string[] entityNames)
        {
            Guard.NotEmpty(prefix, nameof(prefix));
            _urlPrefixes.AddRange(prefix, entityNames);
        }

        public static string GetUrlPrefixFor(string entityName)
        {
            Guard.NotEmpty(entityName, nameof(entityName));

            if (_urlPrefixes.Count == 0)
                return null;

            return _urlPrefixes.FirstOrDefault(x => x.Value.Contains(entityName, StringComparer.OrdinalIgnoreCase)).Key;
        }

        private static bool TryResolveUrlPrefix(string slug, out string urlPrefix, out string actualSlug, out ICollection<string> entityNames)
        {
            urlPrefix = null;
            actualSlug = null;
            entityNames = null;

            if (_urlPrefixes.Count > 0)
            {
                var firstSepIndex = slug.IndexOf('/');
                if (firstSepIndex > 0)
                {
                    var prefix = slug.Substring(0, firstSepIndex);
                    if (_urlPrefixes.ContainsKey(prefix))
                    {
                        urlPrefix = prefix;
                        entityNames = _urlPrefixes[prefix];
                        actualSlug = slug[(prefix.Length + 1)..];
                        return true;
                    }
                }
            }

            return false;
        }

        //private static string NormalizeSlug(RouteValueDictionary routeValues)
        //{
        //    var slug = routeValues[SlugKey] as string;

        //    var lastChar = slug[slug.Length - 1];
        //    if (lastChar == '/' || lastChar == '\\')
        //    {
        //        slug = slug.TrimEnd('/', '\\');
        //        routeValues[SlugKey] = slug;
        //    }

        //    return slug;
        //}

        #endregion

        public override async ValueTask<RouteValueDictionary> TransformAsync(HttpContext httpContext, RouteValueDictionary values)
        {
            // TODO: (core) strip off culture code? Decide once request localization is implemented.
            var slug = httpContext.Request.Path.Value.Trim('/', '\\');

            if (slug.IsEmpty())
            {
                return null;
            }

            if (TryResolveUrlPrefix(slug, out var urlPrefix, out var actualSlug, out var entityNames))
            {
                slug = actualSlug;
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
                // Found slug is outdated. Find the latest active one.
                var activeSlug = await _urlService.GetActiveSlugAsync(urlRecord.EntityId, urlRecord.EntityName, urlRecord.LanguageId);
                if (activeSlug.HasValue())
                {
                    // TODO: (core) Find a way to apply a permanent response redirect at this point
                    return null;
                }
                else
                {
                    // No active slug found
                    return null;
                }
            }

            if (_localizationSettings.SeoFriendlyUrlsForLanguagesEnabled)
            {
                // TODO: (core) Determine request language and - if it differs from urlRecord.LanguageId - 
                // find active slug for requested language, rewrite path and redirect to new location. 
                // ...
            }

            // Verify prefix matches any assigned entity name
            if (entityNames != null && !entityNames.Contains(urlRecord.EntityName, StringComparer.OrdinalIgnoreCase))
            {
                // does NOT match
                return null;
            }

            var transformedValues = Routers.Select(x => x.GetRouteValues(urlRecord, values)).FirstOrDefault();
            if (transformedValues == null)
            {
                return null;
            }

            transformedValues[SlugRouteKey] = slug;
            httpContext.GetRouteData().DataTokens["UrlRecord"] = urlRecord;

            return transformedValues;
        }
    }
}
