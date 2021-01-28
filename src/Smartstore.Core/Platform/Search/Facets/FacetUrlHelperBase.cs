using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Smartstore.Collections;
using Smartstore.Core.Search.Facets;

namespace Smartstore.Core.Platform.Search.Facets
{
    public abstract partial class FacetUrlHelperBase : IFacetUrlHelper
    {
        protected FacetUrlHelperBase(HttpRequest request)
        {
            Guard.NotNull(request, nameof(request));

            Url = request.Path;
            InitialQuery = request.QueryString;
        }

        protected string Url { get; init; }

        protected QueryString InitialQuery { get; init; }

        public abstract int Order { get; }

        public abstract string Scope { get; }

        public virtual async Task<string> AddAsync(params Facet[] facets)
        {
            var qs = new MutableQueryCollection(InitialQuery);

            foreach (var facet in facets)
            {
                var parts = await GetQueryPartsAsync(facet);
                foreach (var name in parts.AllKeys)
                {
                    qs.Add(name, parts[name], !facet.FacetGroup.IsMultiSelect);
                }
            }

            return Url + qs.ToString();
        }

        public virtual async Task<string> RemoveAsync(params Facet[] facets)
        {
            var qs = new MutableQueryCollection(InitialQuery);

            foreach (var facet in facets)
            {
                var parts = await GetQueryPartsAsync(facet);
                foreach (var name in parts.AllKeys)
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
                        var removeValues = parts.GetValues(name);
                        var newValues = currentValues.Except(removeValues).ToArray();
                        if (newValues.Length > 0)
                        {
                            newValues.Each(x => qs.Add(name, x, false));
                        }
                    }
                }
            }

            return Url + qs.ToString();
        }

        public virtual async Task<string> ToggleAsync(Facet facet)
        {
            if (facet.Value.IsSelected)
            {
                return await RemoveAsync(facet);
            }
            else
            {
                return await AddAsync(facet);
            }
        }

        public virtual async Task<string> GetQueryNameAsync(Facet facet)
        {
            var parts = await GetQueryPartsAsync(facet);
            return parts.GetKey(0);
        }

        protected abstract string GetUnmappedQueryName(Facet facet);

        // TODO: (mg) (core) If the result cannot contain duplicate keys: use Dictionary<string, string> instead of NameValueCollection bacause it's just a legacy port.
        protected abstract Task<NameValueCollection> GetQueryPartsAsync(Facet facet);
    }
}
