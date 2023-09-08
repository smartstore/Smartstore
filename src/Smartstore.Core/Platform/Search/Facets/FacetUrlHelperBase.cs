using Microsoft.AspNetCore.Http;
using Smartstore.Collections;
using Smartstore.Core.Seo.Routing;

namespace Smartstore.Core.Search.Facets
{
    public abstract partial class FacetUrlHelperBase : IFacetUrlHelper
    {
        protected FacetUrlHelperBase(HttpRequest request)
        {
            Guard.NotNull(request);

            Url = UrlPolicy.CombineSegments(request.PathBase, request.Path);
            InitialQuery = request.QueryString;
        }

        protected string Url { get; init; }

        protected QueryString InitialQuery { get; init; }

        public abstract int Order { get; }

        public abstract string Scope { get; }

        public virtual string Add(params Facet[] facets)
        {
            // Remove page index (i) from query string.
            var qs = new MutableQueryCollection(InitialQuery)
                .Remove("i");

            foreach (var facet in facets)
            {
                var parts = GetQueryParts(facet);
                foreach (var name in parts.Keys)
                {
                    qs.Add(name, parts[name], !facet.FacetGroup.IsMultiSelect);
                }
            }

            return Url + qs.ToString();
        }

        public virtual string Remove(params Facet[] facets)
        {
            // Remove page index (i) from query string.
            var qs = new MutableQueryCollection(InitialQuery)
                .Remove("i");

            foreach (var facet in facets)
            {
                var parts = GetQueryParts(facet);
                foreach (var name in parts.Keys)
                {
                    var qsName = name;

                    if (!qs.Store.ContainsKey(name))
                    {
                        // Query string does not contain that name. Try the unmapped name.
                        qsName = GetUnmappedQueryName(facet);
                    }

                    string[] currentValues = null;

                    // The query string value is not necessarily equal to the facet value.
                    // We must skip subsequent lines here to not add the removed value again and again.
                    if (facet.FacetGroup.Kind != FacetGroupKind.Price &&
                        facet.FacetGroup.Kind != FacetGroupKind.Availability &&
                        facet.FacetGroup.Kind != FacetGroupKind.NewArrivals)
                    {
                        if (qs.TryGetValue(qsName, out var rawValue))
                        {
                            currentValues = rawValue.ToString()?
                                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                .Select(x => x.Trim())
                                .ToArray();
                        }
                    }

                    qs.Remove(qsName);

                    if (currentValues != null)
                    {
                        var newValues = parts.TryGetValue(name, out var removeValue)
                            ? currentValues.Where(x => !x.EqualsNoCase(removeValue))
                            : currentValues;

                        if (newValues.Any())
                        {
                            newValues.Each(x => qs.Add(name, x, false));
                        }
                    }
                }
            }

            return Url + qs.ToString();
        }

        public virtual string Toggle(Facet facet)
        {
            if (facet.Value.IsSelected)
            {
                return Remove(facet);
            }
            else
            {
                return Add(facet);
            }
        }

        public virtual string GetQueryName(Facet facet)
        {
            var parts = GetQueryParts(facet);
            return parts.Keys?.FirstOrDefault();
        }

        /// <summary>
        /// Gets the unmapped name of a query part, e.g. "m" if the URL contains the brand\manufacturer name.
        /// </summary>
        protected abstract string GetUnmappedQueryName(Facet facet);

        /// <summary>
        /// Gets a name-to-value map of all query parts.
        /// </summary>
        protected abstract Dictionary<string, string> GetQueryParts(Facet facet);
    }
}
